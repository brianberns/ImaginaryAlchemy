namespace ImaginaryAlchemy

open System
open Browser
open Fable.SimpleJson

/// Information known about a concept.
type ConceptInfo =
    {
        /// Generation in which the concept was created on
        /// this client.
        Generation : int

        /// Server indication that this client was first to
        /// discover the concept.
        IsNew : bool

        /// When the concept was discovered by this client.
        Discovered : DateTime

        /// When the concept was last used by this client.
        LastUsed : DateTime
    }

module ConceptInfo =

    /// Discovers a concept new to this client.
    let discover gen isNew =
        let now = DateTime.Now
        {
            Generation = gen
            IsNew = isNew
            Discovered = now
            LastUsed = now
        }

/// Maps each concept to information about the concept.
type ConceptMap = Map<Concept, ConceptInfo>

type SortMode =
    | Alphabetical
    | ByDiscovered
    | ByLastUsed

/// User settings.
type Settings =
    {
        /// Audio enabled/disabled.
        AudioEnabled : bool

        /// Concept information persisted on this client.
        ConceptMap : ConceptMap

        SortMode : SortMode
    }

module Settings =

    /// Initial settings.
    let private initial =
        let info = ConceptInfo.discover 0 false
        {
            AudioEnabled = true
            ConceptMap =
                [ "Earth"; "Fire"; "Water"; "Air" ]
                    |> Seq.map (fun concept ->
                        concept, info)
                    |> Map
            SortMode = ByDiscovered
        }

    /// Local storage key.
    let private key = "ImaginaryAlchemy"

    /// Saves the given settings.
    let save settings =
        WebStorage.localStorage[key]
            <- Json.serialize<Settings> settings

    /// Answers the current settings.
    let get () =
        let json = WebStorage.localStorage[key]
        if isNull json then
            let settings = initial
            save settings
            settings
        else
            Json.parseAs<Settings>(json)

type Settings with

    /// Current settings.
    static member Current =
        Settings.get()
