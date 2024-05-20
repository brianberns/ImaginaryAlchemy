namespace ImaginaryAlchemy

open System
open System.IO

open Microsoft.Data.Sqlite

/// Database access.
type Database private =
    {
        /// Database connection
        Connection : SqliteConnection
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

    /// Adds a parameter for the given command.
    let private addParm name value (cmd : SqliteCommand) =
        cmd.Parameters.AddWithValue(name, value)
            |> ignore

    /// Opens the alchemy database in the given directory.
    let connect dir =
        let path = Path.Combine(dir, "Alchemy.db")
        let conn = new SqliteConnection($"Data Source={path}")
        conn.Open()
        { Connection = conn }

    /// Creates a transaction.
    let createTransaction db =
        let trans = db.Connection.BeginTransaction()
        {
            new IDisposable with
                member _.Dispose() = trans.Commit()
        }

    /// Fetches the generation number of the given concept,
    /// if it exists.
    let getGeneration db (concept : Concept) =
        use cmd =
            db.Connection.CreateCommand(
                CommandText =
                    "select Generation \
                    from Concept \
                    where Name = $Name;")
        addParm "$Name" SqliteType.Text cmd
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
        use cmd =
            db.Connection.CreateCommand(
                CommandText =
                    "insert into Concept (Name, Generation, First, Second, LastModified) \
                    values ($Name, $Generation, $First, $Second, datetime()) \
                    on conflict (Name) do \
                    update set \
                    Generation = $Generation, \
                    First = $First, \
                    Second = $Second,
                    LastModified = datetime();")
        addParm "$Name" concept cmd
        addParm "$Generation" generation cmd
        addParm "$First" first cmd
        addParm "$Second" second cmd
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
        use cmd =
            db.Connection.CreateCommand(
                CommandText =
                    "select Child \
                    from Combination \
                    where First = $First \
                    and Second = $Second;")
        addParm "$First" first cmd
        addParm "$Second" second cmd
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

    /// Inserts a combination of two concepts.
    let insertCombination
        db
        (first : Concept)
        (second : Concept)
        (childOpt : Option<Concept>) =
        use cmd =
            db.Connection.CreateCommand(
                CommandText =
                    "insert into Combination (First, Second, Child, LastModified) \
                    values ($First, $Second, $Child, datetime());")
        addParm "$First" first cmd
        addParm "$Second" second cmd
        match childOpt with
            | Some child -> addParm "$Child" child cmd
            | None -> ()
        let nRows = cmd.ExecuteNonQuery()
        assert(nRows = 1)
