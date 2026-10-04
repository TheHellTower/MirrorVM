# MirrorVM

MirrorVM is a small .NET virtualizer for learning, inspection, and defensive
reverse-engineering research. Its bytecode, opcode handlers, and rewriting
decisions are explicit; it includes no anti-analysis or concealment features.

## Bytecode example

This bytecode computes `40 + 2` and returns the boxed `Int32` value `42`:

```csharp
byte[] byteCode =
{
    0x02, 0x28, 0x00, 0x00, 0x00, // LoadInt32 40 (four-byte little-endian operand)
    0x02, 0x02, 0x00, 0x00, 0x00, // LoadInt32 2
    0x10                          // Add
};

object result = VirtualMachine.Execute(byteCode, new object[0]);
int sum = (int)result; // 42
```

Each opcode handles its own effects on the VM's single `Stack<object>`. Protected
methods use the same `Execute` entrypoint; a string result, for example, is
cast back to `string` by the rewritten method.

## Development milestones

- [x] **M1 - First protect-and-rewrite path:** Convert a small linear
  subset of static methods to bytecode, run it through the shared VM, and leave
  unsupported methods intact with a reported reason. The initial subset
  includes arithmetic and strings.
- [ ] **M2 - Numeric conversion end to end:** Let converted methods use
  `Int64`/`UInt64`, native integers, and floating-point values the runtime can
  already calculate with. Translate constants and arguments, return the right
  CLR types, then exercise signed, unsigned, checked, and unchecked operations
  through protected methods.
- [ ] **M3 - Control flow:** Translate branches and build enough control
  flow and stack validation to run methods with decisions and loops.
- [ ] **M4 - Method calls:** Add call translation and define how VM code
  passes arguments and results between methods.
- [ ] **M5 - Exception handling:** Translate exception regions, including
  catch, filter, finally, and fault clauses, and define exception propagation
  and stack unwinding in the VM.
- [ ] **M6 - Toward broad CLR compatibility:** Expand CIL and CLR behavior
  coverage, then measure it across .NET Framework 2.0-4.8.1 and modern .NET
  through 10. Full CLR compatibility is a long-term research goal; track gaps
  with tests and a compatibility matrix as the supported scope grows.

## Known limitations

- Conversion currently accepts only static, non-generic methods without
  locals or exception handlers, with `Int32`, `UInt32`, or `String` parameters
  and return types, and up to 256 parameters.
- The supported CIL subset is linear: argument loads, I4 constants, `ldstr`,
  implemented arithmetic operations, `neg`, `nop`, and a final `ret`. Calls
  and branches are not supported.
- A method with an unsupported feature is left unchanged and reported. If no
  methods qualify, no protected assembly is written.
- Runtime arithmetic supports more numeric kinds than method conversion, but
  not all CLR numeric behavior or all CIL conversions, comparisons, bitwise
  operations, shifts, and verification rules.
- Assemblies with a strong-name public key, mixed-mode assemblies, and native
  apphosts are rejected. For modern .NET applications, pass the managed
  `.dll`, not the native apphost.
- Protected assemblies require `MirrorVM.Runtime.dll` beside the output.

## .NET compatibility

Goal: .NET Framework 2.0-4.8.1 and modern .NET through 10. Runtime targets:
`net20`, `netstandard2.0`, `net9.0`; virtualizer and tests: .NET 9. The sample
has been run on Framework 2.0 and 4.8.1, Core 3.1, and .NET 9. Intermediate
Framework releases and .NET 10 have not yet been verified.

## Build, test, and run

```powershell
dotnet restore MirrorVM.sln
dotnet build MirrorVM.sln --configuration Release --no-restore
dotnet test tests/MirrorVM.Tests/MirrorVM.Tests.csproj --configuration Release --no-build
```

Build artifacts go under `Release`. To protect a managed assembly on Windows:

```powershell
MirrorVM.exe C:\path\FileToProtect.dll
```

The sibling output is `FileToProtect_MVM.dll` (or `_MVM.exe` for a managed
Framework executable). Run a modern .NET output with `dotnet` and keep
`MirrorVM.Runtime.dll` beside it.

## Project layout

- `src/MirrorVM`: .NET 9 virtualizer and assembly converter.
- `src/MirrorVM.Runtime`: bytecode interpreter, shared stack, handlers, and
  numeric arithmetic helper.
- `tests/MirrorVM.Tests`: VM and arithmetic tests.
- `Samples/MirrorVM.Sample`: cross-target arithmetic and string sample.
- `docs`: GitHub Pages source; its root page redirects to the blog.
- `docs/blog`: technical notes and diagrams.

## About this project

MirrorVM is a vibecoded, human-guided experiment in how far AI can help build a
VM when a person sets the direction and reviews the work. It is intentionally
kept simple for learning and experimentation, not designed to become a product.
Development will be published in gradual research checkpoints. Each release
will add a limited set of capabilities, document what was tested and what
remains open, and let readers follow and assess the VM's progress over time.

## Blog

The static site is in [`docs/blog`](docs/blog/README.md). GitHub Pages must be
enabled and configured to publish from `docs`; each page's copy-link button
uses its current browser URL.

## License

MirrorVM source is licensed under the GNU Affero General Public License, version
3 or later (`AGPL-3.0-or-later`). See [`LICENSE`](LICENSE). User-provided
reference files under `Samples` retain their existing terms.
