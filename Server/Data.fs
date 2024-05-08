namespace ImaginaryAlchemy

open System
open System.IO

open Microsoft.Data.Sqlite

type Data =
    {
        TryFind : Concept -> Option<int>
        Upsert : Concept -> int -> Concept -> Concept -> unit
    }

module Data =

    let private addParm name dbType (cmd : SqliteCommand) =
        cmd.Parameters.Add(name, dbType)
            |> ignore

    let connect dir =

        let path = Path.Combine(dir, "Alchemy.db")
        let conn = new SqliteConnection($"Data Source={path}")
        conn.Open()

        let tryFindCmd =
            let cmd =
                conn.CreateCommand(
                    CommandText =
                        "select Generation \
                        from Concept \
                        where Name = $Name;")
            addParm "$Name" SqliteType.Text cmd
            cmd

        let tryFind (concept : Concept) =
            tryFindCmd.Parameters["$Name"].Value <- concept
            let value = tryFindCmd.ExecuteScalar()
            if isNull value then None
            else Some (Convert.ToInt32 value)

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
            TryFind = tryFind
            Upsert = upsert
        }
