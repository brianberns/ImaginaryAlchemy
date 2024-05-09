namespace ImaginaryAlchemy

open System
open System.IO

open Microsoft.Data.Sqlite

/// Database access.
type Database private =
    {
        /// Database connection
        Connection : SqliteConnection

        /// Command to fetch generation number of a concept.
        GetGenerationCmd : SqliteCommand

        /// Command to insert/update a concept.
        UpsertCmd : SqliteCommand
    }

    interface IDisposable with
        member db.Dispose() =
            db.Connection.Dispose()

module Data =

    (*
     * The Concept table tracks the lowest known generation
     * number for each concept discovered.
     *
     *   Name   | Generation | First | Second
     *  --------+------------+-------+--------
     *   Earth  |          0 |       |
     *   Air    |          0 |       |
     *   Fire   |          0 |       |
     *   Water  |          0 |       |
     *   Steam  |          1 | Fire	 | Water
     *   Geyser |          2 | Earth | Steam
     *)

    /// Creates a parameter for the given command.
    let private addParm name dbType (cmd : SqliteCommand) =
        cmd.Parameters.Add(name, dbType)
            |> ignore

    /// Opens the alchemy database in the given directory.
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

        {
            Connection = conn
            GetGenerationCmd = getGenCmd
            UpsertCmd = upsertCmd
        }

    /// Fetches the generation number of the given concept,
    /// if it exists.
    let getGeneration db (concept : Concept) =
        db.GetGenerationCmd.Parameters["$Name"].Value <- concept
        let value = db.GetGenerationCmd.ExecuteScalar()
        if isNull value then None
        else Some (Convert.ToInt32 value)

    /// Inserts/updates the given concept.
    let upsert
        db
        (concept : Concept)
        (generation : int)
        (firstParent : Concept)
        (secondParent : Concept) =
        db.UpsertCmd.Parameters["$Name"].Value <- concept
        db.UpsertCmd.Parameters["$Generation"].Value <- generation
        db.UpsertCmd.Parameters["$First"].Value <- firstParent
        db.UpsertCmd.Parameters["$Second"].Value <- secondParent
        let nRows = db.UpsertCmd.ExecuteNonQuery()
        assert(nRows = 1)
