#!/usr/bin/env node
// PreToolUse hook for the test-generator, test-from-codegen and test-healer subagents (matcher "Edit|Write|Bash").
//
// Edit and Write: blocks edits that ADD sleeps, fixed waits, retries, NetworkIdle waits or longer timeouts to the
// test and framework code (tests/**/*.cs, src/**/*.cs) or raise a timeout in a .runsettings file. Removing them is
// always allowed, so the healer can delete the ones a flaky test already has. Other files are not checked.
// Bash: blocks in-place shell edits of .cs and .runsettings files (sed -i, perl -i, redirection, tee), which the
// checks above can't see, and asks for the Edit tool instead.
//
// Exit 2 blocks the tool call and shows stderr to the agent. Any other problem exits 1, which Claude Code treats
// as a non-blocking error, so a broken hook never stops legitimate work.
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';

const csharpRules = [
  { label: 'Thread.Sleep', pattern: /\bThread\.Sleep\s*\(/g },
  { label: 'Task.Delay', pattern: /\bTask\.Delay\s*\(/g },
  { label: 'WaitForTimeoutAsync', pattern: /\bWaitForTimeoutAsync\s*\(/g },
  { label: '[Retry]', pattern: /\bRetry\s*\(/g },
  { label: 'NetworkIdle', pattern: /\bNetworkIdle\b/g },
  { label: 'a Timeout option', pattern: /\bTimeout\s*=(?!=)/g },
  { label: 'SetDefaultTimeout / SetDefaultNavigationTimeout', pattern: /\bSetDefault(?:Navigation)?Timeout\s*\(/g },
];

function count(text, pattern) {
  return (text.match(pattern) ?? []).length;
}

function timeoutValues(text) {
  const values = {};
  for (const match of text.matchAll(/<(\w*Timeout)>\s*(\d+)\s*</g)) {
    values[match[1]] = Number(match[2]);
  }
  return values;
}

function block(file, what) {
  process.stderr.write(
    `Blocked by .claude/hooks/guard-test-edits.mjs: this edit adds ${what} to ${file}.\n` +
      'This repo fixes failing and flaky tests at their cause. Sleeps, fixed waits, [Retry], NetworkIdle waits and ' +
      'longer timeouts are not accepted as fixes or as waits.\n' +
      'Wait for the real condition instead: a web-first assertion (Expect(...)), the page object\'s ReadyIndicator, ' +
      'an element the app shows when it is done, or Page.RunAndWaitForResponseAsync.\n' +
      'If you are convinced this is necessary, do not make the edit; explain why in your report.\n',
  );
  process.exit(2);
}

let input;
try {
  input = JSON.parse(readFileSync(0, 'utf8'));
} catch (error) {
  process.stderr.write(`guard-test-edits: could not parse hook input: ${error.message}\n`);
  process.exit(1);
}

const toolName = input.tool_name;
const toolInput = input.tool_input ?? {};

if (toolName === 'Bash') {
  const command = typeof toolInput.command === 'string' ? toolInput.command : '';
  const guardedFile = /[\w./\\-]+\.(?:cs|runsettings)\b/i;
  const inPlaceEditor = /\b(?:sed|perl)\b[^|;&]*?\s-(?:\w*i\w*|-in-place)\b/;
  const redirectIntoGuarded = /(?:>>?|\btee\b(?:\s+-a)?)\s*["']?[\w./\\:-]+\.(?:cs|runsettings)\b/i;
  if (redirectIntoGuarded.test(command) || (inPlaceEditor.test(command) && guardedFile.test(command))) {
    process.stderr.write(
      'Blocked by .claude/hooks/guard-test-edits.mjs: change .cs and .runsettings files with the Edit tool ' +
        '(or Write for a new file), not with sed, perl, tee or shell redirection, so the change can be checked ' +
        'for sleeps, fixed waits, [Retry], NetworkIdle waits and longer timeouts.\n',
    );
    process.exit(2);
  }
  process.exit(0);
}

const filePath = toolInput.file_path;
if (!['Edit', 'Write'].includes(toolName) || typeof filePath !== 'string') {
  process.exit(0);
}

const projectDir = process.env.CLAUDE_PROJECT_DIR || input.cwd || process.cwd();
const relative = path.relative(projectDir, filePath).split(path.sep).join('/');
const isCsharp = /^(tests|src)\/.*\.cs$/i.test(relative);
const isRunSettings = /\.runsettings$/i.test(relative);
if (!isCsharp && !isRunSettings) {
  process.exit(0);
}

let before;
let after;
if (toolName === 'Edit') {
  before = toolInput.old_string ?? '';
  after = toolInput.new_string ?? '';
} else {
  before = existsSync(filePath) ? readFileSync(filePath, 'utf8') : '';
  after = toolInput.content ?? '';
}

if (isCsharp) {
  for (const rule of csharpRules) {
    if (count(after, rule.pattern) > count(before, rule.pattern)) {
      block(relative, rule.label);
    }
  }
} else {
  const oldValues = timeoutValues(before);
  for (const [name, value] of Object.entries(timeoutValues(after))) {
    if (!(name in oldValues) || value > oldValues[name]) {
      block(relative, `a higher ${name}`);
    }
  }
}

process.exit(0);
