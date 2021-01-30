#r "bin/Debug/net5.0/FSharp.Data.dll"
#load "Common.fs"
#load "Twitter.fs"


open System
open Twitter


// fsharplint:disable Hints
let tweets = TweetParser.GetSamples()
let tdict = tweets2dict tweets

convall tdict


convone tdict (DateTime(2014, 3, 13))
convone tdict (DateTime(2014, 3, 8))


