module Common

open System.IO
open System.Diagnostics

// let unimemoRoot ="../../../Google ドライブ/notes"
let unimemoRoot ="../../../GoogleDriveMirror/notes"
let mdroot = $"{unimemoRoot}"

let ensureDir (di: DirectoryInfo) =
  if not di.Exists then
      Directory.CreateDirectory di.FullName |> ignore

let saveText (fi:FileInfo) text =
  File.WriteAllText(fi.FullName, text)


let shellExecute cmdname args=
    use proc = new Process()
    proc.StartInfo.FileName <- cmdname
    proc.StartInfo.Arguments <- args
    proc.StartInfo.RedirectStandardOutput <- true
    proc.Start() |> ignore
    let ret = proc.StandardOutput.ReadToEnd()
    proc.WaitForExit()
    ret


