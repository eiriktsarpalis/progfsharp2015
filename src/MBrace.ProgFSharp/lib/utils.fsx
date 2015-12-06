namespace global

#nowarn "1189"

[<AutoOpen>]
module Utils =

    /// Please add an implementation here
    let __IMPLEMENT_ME__ <'T> : 'T = raise <| new System.NotImplementedException("Don't forget to add an implementation!")

    module Array = 
        let chunkBySize (n:int) (numbers: 'T[])  =
            if n <= 0 then invalidArg "n" "must be positive."

            [| for i in 1 .. numbers.Length / n  do 
                yield [| for j in ((i-1) * n) .. (i * n - 1) do 
                           yield numbers.[j] |] 
               if numbers.Length % n <> 0 then 
                yield [| for j in (numbers.Length / n) * n .. numbers.Length - 1 do 
                           yield numbers.[j] |] |] 

        let splitInto (n:int) (numbers: 'T[])  = 
            if n <= 0 then invalidArg "n" "must be positive."
            if numbers.Length < n then 
                numbers |> Array.map (fun t -> [| t |]) 
            else
                chunkBySize (numbers.Length / n) numbers