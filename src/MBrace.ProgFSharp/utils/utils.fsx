namespace global

#nowarn "1189"

[<AutoOpen>]
module Utils =

    /// Please add an implementation here
    let (!?) <'T> : 'T = raise <| new System.NotImplementedException("Don't forget to add an implementation!")