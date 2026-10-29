# App map

Facts observed live with playwright-cli at the suite's viewport (1920x1080), for reuse by planning and generation. Re-check anything older than the app's last release.

## Practice Software Testing Toolshop (`BaseUrl`, https://practicesoftwaretesting.com)

### Home (`/`), main navigation only

- Checked: 2026-10-29.
- Main menu (`menubar "Main menu"`): link "Home", button "Categories" (`data-test="nav-categories"`, opens a dropdown), link "Contact", link "Sign in"; language button "Select language" (`data-test="language-select"`).
- Categories dropdown links: Hand Tools (`nav-hand-tools`), Power Tools (`nav-power-tools`), Other (`nav-other`), Special Tools (`nav-special-tools`), Rentals (`nav-rentals`).
- Quirk: `GetByRole(AriaRole.Button)` counts the hidden "Toggle navigation" hamburger only below the 992 px breakpoint. At 1920 px, `.Nth(2)` is the "Categories" toggle; at 800 px it is the hamburger. Positional button locators change meaning with the viewport.
- Readiness and search behaviour: see the comments in `src/PlaywrightChallenge.Framework/Pages/HomePage.cs`.

## Linkin Park official site (https://linkinpark.com, no setting yet)

A third-party production site (Nuxt/Vue, CMS content from Sanity). The suite has no setting for it; see `specs/linkin-park-music.plan.md` for the proposed `LinkinParkUrl`. Elements carry no `data-test` ids, and cards, modals and track rows have no roles, so they need CSS class locators.

### Home (`/`)

- Checked: 2026-10-29.
- Ready: the startup promo modal's close button (`.modal.banner` > its only button). The modal is rendered client-side after hydration (it is absent from the server HTML), about 300 ms after the `load` event, so its appearance proves the page is interactive.
- Key locators: main navigation links by role and name, for example `GetByRole(AriaRole.Link, new() { Name = "Music", Exact = true })` (`/music`); also "Tour" (`/tour`), "About" (`/about`), "Videos", "LPU" and "UNSHATTER MOVIE" (external). "Archive" and "Store" are clickable `generic` elements, not links.
- Startup modal: `div.modal.banner.square`, a direct child of `body`, with no role or `aria-modal`. Its close button (`.close > button`) has an SVG icon and no accessible name. The modal covers the main navigation, so a click on "Music" is intercepted until it is closed. Closing removes it from the DOM and sets `localStorage.hasSeenStartupModal_home`; a fresh browser context always sees it. Content observed: "Unshatter" movie promo with an embedded YouTube player and a "Get Tickets" link.
- Other: Follow and Subscribe buttons; a reCAPTCHA frame; a Termly consent manager ("Consent Preferences" in the footer). No consent banner appeared in this session; it may be region-dependent.

### Music (`/music`)

- Checked: 2026-10-29.
- Reached by client-side navigation from the home page. The URL changes first; the list renders about 0.9 s later, after `GET /api/content/page/music` and `GET /api/content/releases` (the whole catalogue, 64 releases with tracklists).
- Ready: the first release card heading (`.release-card` h3). The cards and the default active filter appear in the same render, with no unfiltered intermediate state.
- Filters: buttons "Live", "Albums", "Eps & Singles" and "All Releases" (`GetByRole(AriaRole.Button, new() { Name = ..., Exact = true })`). "Albums" is the default; the active one is marked only by the CSS class `active` (no `aria-pressed`).
- Release cards: `div.release-card` (whole card is clickable, no link role) containing an image, `h3.title` (heading level 3), `.release-date` (for example "17 Jun 2014") and `.type` ("Albums"). Under "Albums", 15 cards, newest first: "FROM ZERO (DELUXE EDITION)", "From Zero", "Papercuts", ..., "The Hunting Party" (17 Jun 2014), ..., "Hybrid Theory".
- Quirk: release titles are not unique across types: "Wastelands" is an album track and also a single under "Eps & Singles".

### Release details modal (on `/music`)

- Checked: 2026-10-29.
- Opened by clicking a release card or its heading; the URL stays `/music` and no request is made (data comes from `/api/content/releases`).
- Root: `div.modal.release`, same modal component as the home startup modal (unnamed close button in `.close`).
- Content: `p.title` (not a heading), `.release-date`, `.type`, "Tracks: N |" and "Time: N MIN", the record label paragraph, link "Buy / Listen" (external short link), then the tracklist: `div.track` rows with `span.track-number`, `span.track-title` and `span.track-length`. Track rows have no click handler (cursor `auto`, clicking changes nothing).
- Example: "The Hunting Party", 17 Jun 2014, Albums, "Tracks: 12 |", "Time: 45 MIN", Warner Bros.; track 6 "Wastelands", 3:15.
