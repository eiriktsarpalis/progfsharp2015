namespace global

[<AutoOpen>]
module Utils =

    let __add_me__<'T> : 'T = raise <| new System.NotImplementedException("Don't forget to fill out this implementation!")