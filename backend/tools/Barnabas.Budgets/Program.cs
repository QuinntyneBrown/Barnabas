using Barnabas.Budgets;

// A console harness rather than an acceptance test, deliberately.
//
// L2-103 and L2-107 assert wall-clock budgets under load. Those can fail on a developer machine
// for reasons that have nothing to do with correctness - a build running in another window, a
// laptop on battery, a SQL Express instance that has just started and has nothing cached - and a
// suite that goes red for those reasons teaches people to ignore it. So they live here, out of
// `dotnet test`, and are run deliberately against a running API:
//
//     cd backend/src/Barnabas.Api && dotnet run
//     cd backend && dotnet run --project tools/Barnabas.Budgets
//
// Every measurement is reported, not only whether it passed. The exit code is 1 if any budget was
// missed, so this can gate a deployment even though it does not gate a commit.
return await Harness.RunAsync(args);
