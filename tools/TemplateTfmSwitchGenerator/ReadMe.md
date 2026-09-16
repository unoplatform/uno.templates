# TemplateTfmSwitchGenerator

Owns the target frameworks the templates emit.

`TemplateTfms.cs` is the single source of truth:

| Member | Controls |
| --- | --- |
| `WindowsPlatformVersion` | the platform version of every Windows TFM (`netX.0-windows<version>`) |
| `Runtimes` | the .NET versions offered by the `tfm` template parameter |
| `Platforms` | the platform conditions expanded into switch cases |

Running the tool applies those values to:

- the `singleProjectTfms` generated switch symbol in
  `src/Uno.Templates/content/unoapp/.template.config/template.json` (patched in place,
  the rest of the file is left untouched);
- every hand-written Windows TFM in `src/Uno.Templates/content` — currently
  `.run/MyExtensionsApp.1.run.xml`, `MyExtensionsApp.1.MauiControls.csproj` and
  `unolib/CrossTargetedLibrary.csproj`.

## Usage

Update the templates after changing `TemplateTfms.cs`:

```shell
dotnet run --project tools/TemplateTfmSwitchGenerator
```

Report drift without writing anything (exits with `1` when the templates are out of date):

```shell
dotnet run --project tools/TemplateTfmSwitchGenerator -- --check
```

Do not hand-edit the generated switch cases or the Windows TFMs listed above — change
`TemplateTfms.cs` and re-run the tool instead.
