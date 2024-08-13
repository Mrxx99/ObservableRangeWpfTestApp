# ObservableRangeWpfTestApp

This is a small app to test INotifyCollectionChanged notifications with ranges for WPF.
Mainly created to test this PR to WPF: https://github.com/dotnet/wpf/pull/9568

- It requires .NET 9 (previews).
The range operations Add/Remove/Replace currently throw an exception that range operations are not supported.

- To test with WPF sources, uncomment the commented code in the csproj, give the path to the cloned [WPF source](https://github.com/dotnet/wpf).
  - Than build the WPF source with `./build.cmd -c debug -plat x64`.
  - After this changes can be done in the `PresentationFramwork` project of WPF source.
  - When changing the Configuration to x64 for the WPF sln, it is enough from there on to only build the `PresentationFramework` project in your IDE.
  - After doing changes, always rebuild this test app before running again.
  - You can easily debug into WPF sources when setting a breakpoint in `ObservableRangeCollection.OnCollectionChanged` and stepping into  `handler(this, e);`
    (Maybe your IDE has to be configured to allow stepping into different sources).

**Tip**: If you want to ensure local WPF sources are used with this test app, find the `Window.cs` in `PresentationFramework` and add a MessageBox to the Show method.
