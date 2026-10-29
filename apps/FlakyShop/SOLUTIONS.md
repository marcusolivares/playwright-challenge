# Flaky Shop: solutions

> Spoilers. Try the exercises in [README.md](README.md) first.

All of these fixes were checked by running the lab suite 5 times in a row (30 of 30 passed).

---

## 1. Orders: a fixed wait and a one-time read

```csharp
await _ordersPage.LoadOrdersAsync();
await Page.WaitForTimeoutAsync(1000);                          // "the API is slow; give it a second"
Assert.That(await _ordersPage.Rows.CountAsync(), Is.EqualTo(5));
```

**Evidence.** The error says `Expected: 5, But was: 0`. In the trace, the `CountAsync` step runs while the
`/api/orders` request in the Network tab is still pending. That request takes 300–1700 ms.

**Root cause.** A fixed wait is a guess about how long the server will take. `CountAsync()` reads the DOM **once** and
doesn't retry. `IsVisibleAsync()`, `TextContentAsync()`, `InnerTextAsync()` and `AllInnerTextsAsync()` behave the same
way.

**Fix.** Use a web-first assertion, which keeps retrying until it passes or reaches `ExpectTimeout`:

```csharp
await _ordersPage.LoadOrdersAsync();
await Expect(_ordersPage.Rows).ToHaveCountAsync(5);
```

**What to say.** "Raising the wait to 2 s would only make the test pass more often. It would still fail on a slow day,
and it costs 2 s on every fast run. I'd rather wait for the actual condition."

---

## 2. Catalog: a selector that depends on position

```csharp
var position = Array.IndexOf(CatalogOrder, productName) + 1;
return Page.Locator($".cards > li:nth-child({position}) button").ClickAsync();
```

**Evidence.** The cart contains a different product. In the trace, the snapshot before the click shows the grid in a
different order than `CatalogOrder` expects, and the click lands on another product's button. The page says
"Sorted by popularity", and that ranking changes.

**Root cause.** The locator finds the product by its **position**, not by **what it is**. It's also CSS, which the
team's locator priority puts last.

**Fix** (in the page object, where selectors belong):

```csharp
public Task AddToCartAsync(string productName) =>
    ProductCards
        .Filter(new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = productName, Exact = true }) })
        .GetByRole(AriaRole.Button, new() { Name = "Add to cart" })
        .ClickAsync();
```

`Exact = true` matters here. Without it, a name like "saw" would match both "Hand saw" and "Circular saw". With the
fix, the static `CatalogOrder` array isn't needed any more.

---

## 3. Team tasks: shared test data

```csharp
private const string Title = "Write report";
...
await Expect(_tasksPage.Tasks).ToHaveCountAsync(1);
```

**Evidence.** `Locator expected to have count '1' But was: '10'`, and a strict mode violation:
`...resolved to 8 elements`. Both tests pass one at a time on a freshly restarted app.

**Root cause.** The two tests share data with each other and with every earlier run. They create tasks with the
same title, and one of them asserts on the **whole list** instead of only the task it created. NUnit runs tests in
alphabetical order, so `Completing_...` runs first and leaves a task behind for `New_task_...`.

**Fix.** Each test creates its own uniquely named data and asserts only on that:

```csharp
[Test]
public async Task New_task_appears_in_the_list()
{
    var title = $"Write report {UniqueId.Next()}";
    await _tasksPage.AddAsync(title);

    await Expect(_tasksPage.TaskNamed(title)).ToHaveCountAsync(1);
}
```

**NUnit gotcha.** Don't move the unique title into a field (`private readonly string _title = ...`). NUnit creates
**one instance of the test class for all its tests**, so both tests would get the same "unique" title and fail again.
xUnit is different: it creates a new instance for each test. Use a local variable, or set the field in `[SetUp]`.

**Fixes that don't work.** Deleting all tasks in `[SetUp]` breaks as soon as tests run in parallel, or if anyone else
uses the same environment. Restarting the app before each run only hides the problem.

---

## 4. Checkout: clicking before the page is ready

```csharp
protected override ILocator ReadyIndicator => _heading;   // CheckoutPage
```

**Evidence.** The click succeeds (it's green in the trace), but nothing happens afterwards. In the Network tab, the
click comes **before** the `/api/payment-methods` response, and no `/api/checkout` request is ever sent. The
screenshot shows no payment options. The test fails whenever that API is slow, which is about half the time.

**Root cause.** The button is visible and enabled, so Playwright's checks before clicking pass. But the app only
attaches the button's click handler after the payment methods load. Playwright can't tell that from the page. The page
object treated the page as ready as soon as the heading appeared, which happens too early.

**Fix.** Wait for the thing a real user would wait for, in the page object:

```csharp
protected override ILocator ReadyIndicator => Page.GetByRole(AriaRole.Radio).First;
```

Another fix that also reads well is to have the test choose a payment method first, because that action waits for the
radio buttons:

```csharp
await _checkoutPage.PaymentMethod("Card").CheckAsync();
```

**What to tell the developers.** This is also a product bug. A real user who clicks quickly loses their click. The
button should be disabled until the page has finished loading.

---

## 5. Search: two responses racing each other

```csharp
// SearchPage
protected override ILocator ReadyIndicator => _searchBox;

// test
await _searchPage.SearchAsync("saw");
await Expect(_searchPage.Results.First).ToBeVisibleAsync();
var names = await _searchPage.Results.AllInnerTextsAsync();
Assert.That(names, Has.All.Contains("saw").IgnoreCase);
```

**Evidence.** When the test fails, `names` holds all 8 products. Step through the trace timeline:

- **Fast server.** The full product list has already rendered before the search. `Results.First` is visible right
  away, so the snapshot holds the full list → **FAIL**.
- **Slow server.** The full list hasn't loaded yet. The search results (400 ms) arrive first and the snapshot holds
  two saws → **PASS**. But at about 1500 ms the full list arrives and **replaces the search results**. Look at the
  final DOM snapshot in the trace: the test passed while the screen was wrong.

**Root causes** (there are two):

1. *Test:* `ToBeVisibleAsync` on `.First` only proves that some list is showing, not that the search results are.
   `AllInnerTextsAsync` then reads the DOM once at that moment.
2. *Page object and app:* the test searches before the page has finished loading. The app renders whichever response
   arrives last. It doesn't cancel the earlier request, so a slow initial load overwrites the search results.

**Fix:**

```csharp
// SearchPage: the page is ready once the initial list has rendered
protected override ILocator ReadyIndicator => Results.First;

// test: one assertion that keeps retrying until the exact expected list appears
await _searchPage.SearchAsync("saw");
await Expect(_searchPage.Results).ToHaveTextAsync(["Hand saw", "Circular saw"]);
```

**What to tell the developers.** Out-of-order responses are a real bug. The fix is to cancel the old request with an
`AbortController`, or to ignore responses for anything but the latest query. The existing `HomePage` in this repo
works around the same problem on the public demo site; see the comment on its `ReadyIndicator`.

---

## Why not just `[Retry(3)]`?

- **It hides product bugs.** Exercises 4 and 5 are real problems that users would hit.
- **It doesn't fix the failure rate.** A test that fails 50% of the time still fails all three attempts 12.5% of the
  time. Exercise 3 fails on every attempt.
- **It slows everything down.** Each failed attempt waits for the full timeout first.
- **It teaches the team to ignore red builds.**

Retries can be acceptable **temporarily** while the cause is being fixed, but only if each retried test is tracked in
a ticket and someone is responsible for it.

C# detail: by default, NUnit's `[Retry]` only retries **NUnit** assertion failures (`Assert.That`). A failed Playwright
`Expect(...)` throws `PlaywrightException`, and a timed-out action throws `TimeoutException`. Neither is retried, so in
this suite `[Retry(3)]` wouldn't retry most of these tests.
