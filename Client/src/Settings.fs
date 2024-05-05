namespace ImaginaryAlchemy

open System
open Browser
open Fable.SimpleJson

type ConceptInfo =
    {
        Generation : int
        Discovered : DateTime
    }

module ConceptInfo =

    let create gen =
        let now = DateTime.Now
        {
            Generation = gen
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
        {
            AudioEnabled = true
            ConceptMap =
                Map [
                    "Earth", ConceptInfo.create 0
                    "Fire", ConceptInfo.create 0
                    "Water", ConceptInfo.create 0
                    "Air", ConceptInfo.create 0
                ]
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
