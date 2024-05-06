namespace ImaginaryAlchemy

open System
open Browser
open Fable.SimpleJson

type ConceptInfo =
    {
        Generation : int
        IsNew : bool
        Discovered : DateTime
    }

module ConceptInfo =

    let create gen isNew =
        let now = DateTime.Now
        {
            Generation = gen
            IsNew = isNew
            Discovered = now
        }

type ConceptMap =
    Map<Concept, ConceptInfo>

/// User settings.
type Settings =
    {
        /// Audio enabled/disabled.
        AudioEnabled : bool

        ConceptMap : ConceptMap
    }

module Settings =

    /// Initial settings.
    let initial =
        let info = ConceptInfo.create 0 false
        {
            AudioEnabled = true
            ConceptMap =
                [
                    "Earth"
                    "Fire"
                    "Water"
                    "Air"
                ]
                    |> Seq.map (fun concept -> concept, info)
                    |> Map
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
