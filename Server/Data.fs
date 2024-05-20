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
        UpsertConceptCmd : SqliteCommand
    }

    interface IDisposable with
        member db.Dispose() =
            db.Connection.Dispose()

module Data =

    (*
     * The Concept table has one row for each known concept:
     *
     *   Name   | Generation | First | Second
     *  --------+------------+-------+--------
     *   Earth  |          0 |       |
     *   Air    |          0 |       |
     *   Fire   |          0 |       |
     *   Water  |          0 |       |
     *   Steam  |          1 | Fire	 | Water
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
                        "insert into Concept (Name, Generation, First, Second, LastModified) \
                        values ($Name, $Generation, $First, $Second, datetime()) \
                        on conflict (Name) do \
                        update set \
                        Generation = $Generation, \
                        First = $First, \
                        Second = $Second,
                        LastModified = datetime();")
            addParm "$Name" SqliteType.Text cmd
            addParm "$Generation" SqliteType.Integer cmd
            addParm "$First" SqliteType.Text cmd
            addParm "$Second" SqliteType.Text cmd
            cmd

        {
            Connection = conn
            GetGenerationCmd = getGenCmd
            UpsertConceptCmd = upsertCmd
        }

    /// Fetches the generation number of the given concept,
    /// if it exists.
    let getGeneration db (concept : Concept) =
        db.GetGenerationCmd.Parameters["$Name"].Value <- concept
        let value = db.GetGenerationCmd.ExecuteScalar()
        if isNull value then None
        else Some (Convert.ToInt32 value)

    /// Inserts/updates the given concept.
    let upsertConcept
        db
        (concept : Concept)
        (generation : int)
        (firstParent : Concept)
        (secondParent : Concept) =
        db.UpsertConceptCmd.Parameters["$Name"].Value <- concept
        db.UpsertConceptCmd.Parameters["$Generation"].Value <- generation
        db.UpsertConceptCmd.Parameters["$First"].Value <- firstParent
        db.UpsertConceptCmd.Parameters["$Second"].Value <- secondParent
        let nRows = db.UpsertConceptCmd.ExecuteNonQuery()
        assert(nRows = 1)
