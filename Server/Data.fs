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

        GetCombinationCmd : SqliteCommand
        InsertCombinationCmd : SqliteCommand
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
     *
     * The Combination has one row for each combination that's been
     * tried:
     *
     *  First | Second | Child
     * -------+--------+-------
     *  Fire  | Water  | Steam
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
        let upsertConceptCmd =
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

            // command to get a combination
        let getCombinationCmd =
            let cmd =
                conn.CreateCommand(
                    CommandText =
                        "select Child \
                        from Combination \
                        where First = $First \
                        and Second = $Second;")
            addParm "$First" SqliteType.Text cmd
            addParm "$Second" SqliteType.Text cmd
            cmd

            // command to insert a combination
        let insertCombinationCmd =
            let cmd =
                conn.CreateCommand(
                    CommandText =
                        "insert into Combination (First, Second, Child, LastModified) \
                        values ($First, $Second, $Child, datetime());")
            addParm "$First" SqliteType.Text cmd
            addParm "$Second" SqliteType.Text cmd
            addParm "$Child" SqliteType.Text cmd
            cmd

        {
            Connection = conn
            GetGenerationCmd = getGenCmd
            UpsertConceptCmd = upsertConceptCmd
            GetCombinationCmd = getCombinationCmd
            InsertCombinationCmd = insertCombinationCmd
        }

    /// Fetches the generation number of the given concept,
    /// if it exists.
    let getGeneration db (concept : Concept) =
        let cmd = db.GetGenerationCmd
        cmd.Parameters["$Name"].Value <- concept
        let value = cmd.ExecuteScalar()
        if isNull value then None
        else Some (Convert.ToInt32 value)

    /// Inserts/updates the given concept.
    let upsertConcept
        db
        (concept : Concept)
        (generation : int)
        (first : Concept)
        (second : Concept) =
        let cmd = db.UpsertConceptCmd
        cmd.Parameters["$Name"].Value <- concept
        cmd.Parameters["$Generation"].Value <- generation
        cmd.Parameters["$First"].Value <- first
        cmd.Parameters["$Second"].Value <- second
        let nRows = cmd.ExecuteNonQuery()
        assert(nRows = 1)

    /// Gets the combination of the two given concepts.
    /// * None: No row exists (yet) for this combination
    /// * Some None: Parents cannot be combined
    /// * Some concept: Parents can be comined
    let getCombination
        db
        (first : Concept)
        (second : Concept) =
        let cmd = db.GetCombinationCmd
        cmd.Parameters["$First"].Value <- first
        cmd.Parameters["$Second"].Value <- second
        use reader = cmd.ExecuteReader()
        let rows =
            [|
                while reader.Read() do
                    if reader.IsDBNull(0) then None
                    else reader.GetString(0) |> Some
            |]
        match rows.Length with
            | 0 -> None
            | 1 -> Some rows[0]
            | _ -> failwith "Unexpected"

    let insertCombination
        db
        (first : Concept)
        (second : Concept)
        (conceptOpt : Option<Concept>) =
        let cmd = db.InsertCombinationCmd
        cmd.Parameters["$First"].Value <- first
        cmd.Parameters["$Second"].Value <- second
        cmd.Parameters["$Child"].Value <-
            match conceptOpt with
                | Some concept -> box concept
                | None -> DBNull.Value
        let nRows = cmd.ExecuteNonQuery()
        assert(nRows = 1)
