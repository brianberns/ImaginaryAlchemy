namespace ImaginaryAlchemy

open System
open System.IO

open Microsoft.Data.Sqlite

/// Data access API.
type Data =
    {
        /// Fetches generation number of the given concept,
        /// if it exists.
        GetGeneration : Concept -> Option<int>

        /// Inserts the given concept.
        Upsert :
            (*child concept*) Concept
            -> (*generation*) int
            -> (*parent concept*) Concept
            -> (*parent concept*) Concept
            -> unit
    }

module Data =

    /// Creates a parameter for the given command.
    let private addParm name dbType (cmd : SqliteCommand) =
        cmd.Parameters.Add(name, dbType)
            |> ignore

    /// Connects to the alchemy database in the given directory.
    let connect dir =

            // open database connection
        let path = Path.Combine(dir, "Alchemy.db")
        let conn = new SqliteConnection($"Data Source={path}")
        conn.Open()

            // command to get a concept's generation number
        let getGenCmd =
            let cmd =
                conn.CreateCommand(
                    CommandText =
                        "select Generation \
                        from Concept \
                        where Name = $Name;")
            addParm "$Name" SqliteType.Text cmd
            cmd

            // fetches generation number of the given concept
        let getGen (concept : Concept) =
            getGenCmd.Parameters["$Name"].Value <- concept
            let value = getGenCmd.ExecuteScalar()
            if isNull value then None
            else Some (Convert.ToInt32 value)

            // command to insert/update a concept
        let upsertCmd =
            let cmd =
                conn.CreateCommand(
                    CommandText =
                        "insert into Concept (Name, Generation, First, Second) \
                        values ($Name, $Generation, $First, $Second) \
                        on conflict (Name) do \
                        update set \
                        Generation = $Generation, \
                        First = $First, \
                        Second = $Second;")
            addParm "$Name" SqliteType.Text cmd
            addParm "$Generation" SqliteType.Integer cmd
            addParm "$First" SqliteType.Text cmd
            addParm "$Second" SqliteType.Text cmd
            cmd

            // inserts/updates a concept
        let upsert
            (concept : Concept)
            (generation : int)
            (first : Concept)
            (second : Concept) =
            upsertCmd.Parameters["$Name"].Value <- concept
            upsertCmd.Parameters["$Generation"].Value <- generation
            upsertCmd.Parameters["$First"].Value <- first
            upsertCmd.Parameters["$Second"].Value <- second
            let nRows = upsertCmd.ExecuteNonQuery()
            assert(nRows = 1)

        {
            GetGeneration = getGen
            Upsert = upsert
        }
