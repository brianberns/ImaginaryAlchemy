namespace ImaginaryAlchemy

open System.IO
open Microsoft.Extensions.Configuration

/// Server-side settings.
type Settings =
    {
        ApiKey : string
        Secret : string
    }

module Settings =

    /// Gets settings from the given directory.
    let get dir =
        let path =
            Path.Combine(dir, "appsettings.json")
                |> Path.GetFullPath
        ConfigurationBuilder()
            .AddJsonFile(path)
            .Build()
            .Get<Settings>()
