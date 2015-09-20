namespace FSharp.TestLibrary

open Should

module AdditionTests =
    let FirstTest() =
        let r = 4 + 3
        r.ShouldEqual 7

    let SecondTest() =
        let r = 0 + 3
        r.ShouldEqual 3

module SubtractionTests =
    let First() =
        let r = 3 - 2
        r.ShouldEqual 1

    let ``Second Test``()=
        let r = 7 - 2
        r.ShouldEqual 5

    let ``Another test for subtracting things``=
        let r = 5 - 7
        r.ShouldEqual -2