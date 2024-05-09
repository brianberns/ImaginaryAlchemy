namespace ImaginaryAlchemy

open System.IO
open System.Reflection

open Suave
open Suave.Filters
open Suave.Operators

module App =

    /// Web part.
    let app =

            // get path where the server is running
        let dir =
            Assembly.GetExecutingAssembly().Location
                |> Path.GetDirectoryName

            // location of static files
        let staticPath = Path.Combine(dir, "public")

        choose [

                // API calls
            Remoting.webPart dir

                // root file
            Filters.path "/"
                >=> Files.browseFile
                    staticPath "index.html"

                // other static files
            GET >=> Files.browse staticPath
        ]
