## What this does

<!-- One or two sentences. What is different afterwards? -->

**Closes** <!-- L2-0xx, and/or #issue -->

## Decisions taken

<!--
Not what you changed — the diff says that. What you decided, and why the alternatives were worse.
If a choice is counter-intuitive, this is where it earns its place.
-->

## What this deliberately does not do

<!--
Anything you found and did not fix, anything you scoped out, anything left for later. Say so here
rather than leaving it to be discovered. "Nothing" is a fine answer.
-->

## Verification

<!-- Paste the real output. `skipped: 0` is the number reviewers watch. -->

```text
backend   dotnet build   0 warnings
backend   dotnet test    ___ passed, 0 failed, 0 skipped
frontend  playwright     ___ passed, 0 failed, 0 skipped
```

<!--
If you touched anything that could affect them, the budgets too:

  backend   dotnet run --project tools/Barnabas.Budgets
  frontend  npm run build && npx playwright test --config playwright.budgets.config.ts
-->

## Checklist

- [ ] Every new test carries its trace comment — `// Acceptance Test`, `// Traces to: L2-0xx`,
      `// Description: …` — and a test narrower than the requirement it traces to says so.
- [ ] Each acceptance test failed before the implementation and passes after it.
- [ ] `dotnet build` is clean under `TreatWarningsAsErrors`, and no test is skipped.
- [ ] Any rule two callers could race past is decided by a database constraint rather than by a
      read-then-write.
- [ ] No new `IgnoreQueryFilters()` outside a seeder, a test, or a sanctioned store — or this pull
      request says why.
- [ ] The domain's vocabulary is used exactly: listing, board, member, congregation, request,
      neighbourhood.
- [ ] Documentation invalidated by this change has been updated — specs, ADRs, designs, mocks.
