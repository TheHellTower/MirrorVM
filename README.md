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

- [x] **M1 - First protect-and-rewrite path:** Convert a linear arithmetic and
  string method subset, execute it through the shared VM, and leave unsupported
  methods unchanged with a reported reason. The protected sample passed all 26
  configured runtime matrix entries in x86 and x64 processes.
- [x] **M2 - Numeric values end to end:** Carry signed and unsigned integers,
  native integers, `Single`, and `Double` through arguments, constants,
  arithmetic, conversions, and typed returns. Implement arithmetic, bitwise,
  shift, comparison, `ckfinite`, and the full CIL `conv.*` family, including
  checked unsigned-source forms. CLR differential checks for implemented
  numeric opcodes and protected numeric methods passed all 26 configured matrix
  entries in x86 and x64 processes. `Decimal` is supported by value only; its
  arithmetic operators compile to calls.
- [ ] **M3 - Control flow:** Translate branches and build enough control
  flow, branch targets, stack merges, and validation to run methods with
  decisions, loops, and switches.
- [ ] **M4 - Method calls and dispatch:** Add `call`, `callvirt`, and object
  creation with argument and return handling. Cover overloads, virtual and
  abstract methods, interface dispatch, structs, generic methods/types, and the
  `constrained.` and `tail.` prefixes.
- [ ] **M5 - Exception handling:** Translate exception regions, including
  nested catch, filter, finally, and fault clauses, then verify exception
  propagation, rethrow, and stack unwinding.
- [ ] **M6 - Toward broad CLR compatibility:** Cover the remaining type system
  and CIL rules: Boolean and character values; every signed and unsigned integer
  width; native integers; floating point; `Decimal`, `Half`, `Int128`, and
  `UInt128`; enums; references, strings, objects, and boxing; nullable and
  arbitrary structs and layout; arrays of every rank; generics; managed
  byrefs/ref returns; unmanaged and function pointers; typed references; unsafe
  code; and async/iterator state machines. The final gate is the full version
  matrix below. Every milestone remains open until its behavior is built and run
  on every applicable target.

## Known limitations

- Conversion currently accepts only static, non-generic methods without
  locals or exception handlers and with up to 256 parameters. Supported
  parameter and return types are Boolean, the built-in integer types through
  64-bit, `IntPtr`/`UIntPtr`, `Single`, `Double`, `Decimal`, and `String`.
- The supported CIL subset is linear: argument loads, I4/I8/R4/R8 constants,
  `ldstr`, integer and floating arithmetic, bitwise operations, shifts,
  comparisons, `neg`, `not`, `ckfinite`, all numeric `conv.*` opcodes, `nop`,
  and a final `ret`. Calls, branches, fields, arrays, and pointer/byref
  operations are not supported yet.
- A method with an unsupported feature is left unchanged and reported. If no
  methods qualify, no protected assembly is written.
- `Decimal` operators and user-defined numeric operators compile to method calls
  and are skipped until call support. Enums, arbitrary structs, object and array
  references, and unsafe or managed pointers are outside the current subset.
- The converter handles only linear stack flow. Branches, stack merges, locals,
  exception regions, and the broader CLR verification rules are not implemented.
- Assemblies with a strong-name public key, mixed-mode assemblies, and native
  apphosts are rejected. For modern .NET applications, pass the managed
  `.dll`, not the native apphost.
- Protected assemblies require `MirrorVM.Runtime.dll` beside the output.

## .NET compatibility

Goal: build and run on .NET Framework 2.0, 3.0, 3.5, 4.0, 4.5, 4.5.1, 4.5.2,
4.6, 4.6.1, 4.6.2, 4.7, 4.7.1, 4.7.2, 4.8, and 4.8.1; .NET Core 2.0, 2.1,
2.2, 3.0, and 3.1; and .NET 5 through 10. The runtime builds for `net20`,
`netstandard2.0`, and `net9.0`; the virtualizer and unit tests target .NET 9.
The sample builds for the individual SDK-supported targets from Framework 2.0
and 3.5 through 4.8.1, Core 2.0 through 3.1, and .NET 5 through 10. Framework
3.0 has no reference pack in the installed SDK package set; the matrix runner
uses the `net20` sample for its CLR 2.0 execution line.

The Release solution builds all 25 configured sample targets. The protected
sample and its CLR differential checks passed all 26 matrix entries in both
x86 and x64 processes: Framework 2.0/3.0/3.5 and 4.0–4.8.1, Core 2.0–3.1, and
.NET 5–10. Framework 3.0 uses the `net20` sample on the CLR 2.0 line. Framework
4.x targets execute on the installed 4.8.1 in-place CLR, not separate
historical 4.x runtimes. The 16 unit tests pass on .NET 9. Legacy Core targets
produce NuGet vulnerability warnings because those runtimes are out of support.

## Build, test, and run

```powershell
dotnet restore MirrorVM.sln
dotnet build MirrorVM.sln --configuration Release --no-restore
dotnet test tests/MirrorVM.Tests/MirrorVM.Tests.csproj --configuration Release --no-build
.\scripts\verify-runtime-matrix.ps1
.\scripts\verify-runtime-matrix.ps1 -X86
```

The matrix script requires each targeted runtime to be installed for the
selected architecture and fails when one is missing. `-X86` runs the same
matrix at 32-bit width; its Framework runs require the Windows SDK's
`CorFlags.exe`. A selected-runtime run, for example `-TargetFrameworks net9.0`,
is useful locally but does not pass the full gate.
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
- `tests/MirrorVM.Tests`: CLR comparison and protected-method tests.
- `Samples/MirrorVM.Sample`: cross-target protected arithmetic and numeric sample.
- `scripts/verify-runtime-matrix.ps1`: builds and runs the protected sample per target.
- `docs`: GitHub Pages source; its root page redirects to the blog.
- `docs/blog`: technical notes and diagrams.

## About this project

MirrorVM is a vibecoded, human-guided experiment in how far AI can help build a
VM when a person sets the direction and reviews the work. It is intentionally
kept simple for learning and experimentation, not designed to become a product.
Development will be published in gradual research checkpoints. Each release
will add a limited set of capabilities, document what was tested and what
remains open, and let readers follow and assess the VM's progress over time.
Some AI-generated filler ("AI slop") may remain temporarily, mainly because
the human maintainer may be lazy about low-priority cleanup during review. Any
that remains will be removed in the final milestone.

## Blog

Read the [MirrorVM blog](https://thehelltower.github.io/MirrorVM). The static
site is in [`docs/blog`](docs/blog/README.md).

## License

MirrorVM source is licensed under the GNU Affero General Public License, version
3 or later (`AGPL-3.0-or-later`). See [`LICENSE`](LICENSE). User-provided
reference files under `Samples` retain their existing terms.
