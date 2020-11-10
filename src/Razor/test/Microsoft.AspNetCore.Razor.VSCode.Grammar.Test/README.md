# VSCode TextMate Grammar Tests
[Jest](https://jestjs.io/en) is used for VS Code TextMate Grammar unit tests.

## Setup
```bash
yarn install

npm i -g jest
```

**Note:** There are [known issues](https://github.com/nodejs/node-gyp/blob/master/macOS_Catalina.md) with MacOS Catalina for certain dependencies. Windows works well.


## Snapshot Tests
[Jest Snapshot Tests](https://jestjs.io/docs/en/snapshot-testing) are utilized to test out the TextMate grammar in VS Code. As the name suggests, snapshot tests store a serialized _snapshot_ of the tokenized and parsed result of the grammar on a particular test string.

### Running Snapshot Tests
```bash
jest
```

or

```bash
yarn test
```

### Directory Structure
- `./tests`:
  - Contains test suites for the various language structures.
- `./tests/__snapshots__/GrammarTests.test.ts.snap`:
  - Is the `.snap` file which preserves the tokenized and parsed snapshot. Make sure to commit this file with your changes as it saves the expected state of the grammar.
- `./tests/GrammarTests.test.ts`:
  - Is the main test wrapper. Once a new test suite is added, this wrapper must be updated to call the test suite.

### Adding a Test Suite
1. Add new test suite file in `./tests` (you can copy an existing test suite as a template).
2. Update `./tests/GrammarTests.test.ts` with the new test suite.
3. Run `jest --updateSnapshot`

### Adding / Updating a Test in an Existing Test Suite
1. Ensure the grammar is functioning as expected visually using a test string within a `.cshtml` / `.razor` file.
2. Add the test string to the test suite using the form:
```typescript
it('[Test Name]', async () => {
    await assertMatchesSnapshot('[Test String]');
});
```
3.  Run `jest --updateSnapshot` to serialize the tokensized and parsed representation of the test string. This frozen state will be treated as the "source of truth" for future executions of the test suite, in order to identify regressions.
4. Ensure the `./tests/__snapshots__/GrammarTests.test.ts.snap` file is commited with your changes.
