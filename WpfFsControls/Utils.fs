namespace RZ.Wpf

module Utils = 
    let memoize (f: 'a -> 'b) =
        let dict = System.Collections.Concurrent.ConcurrentDictionary<'a,'b>()

        let memoizedFunc input =
            match dict.TryGetValue input with
            | true, x -> x
            | false, _ ->
                let answer = f input
                dict.TryAdd(input, answer) |> ignore
                answer
        memoizedFunc
