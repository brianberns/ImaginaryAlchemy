namespace ImaginaryAlchemy

open Browser
open Fable.SimpleJson

/// User settings.
type Settings =
    {
        /// Audio enabled/disabled.
        AudioEnabled : bool

        ConceptMap : Map<Concept, (*generation*) int>
    }

module Settings =

    /// Initial settings.
    let initial =
        {
            AudioEnabled = true
            ConceptMap =
                Map [
                    "Earth", 0
                    "Fire", 0
                    "Water", 0
                    "Air", 0
                    "Steam", 1
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
