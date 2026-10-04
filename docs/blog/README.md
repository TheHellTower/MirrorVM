# MirrorVM Field Notes

This folder is the source for the project's separate static technical blog.
Open `index.html` in a browser to read it. The pages have no build step or
external dependencies; a small local script copies the current page URL when a
reader selects the copy-link button.

The parent `docs/index.html` redirects to this folder. The `docs` directory is
ready to use as a GitHub Pages publishing source; Pages still needs to be
enabled and configured in the repository settings.

The three posts cover the current protect-and-rewrite path, arithmetic and
strings through serialized bytecode, and the compatibility target matrix.

The compatibility goal spans .NET Framework 2.0 through 4.8.1 and modern .NET.
The runtime currently targets `net20`, `netstandard2.0`, and `net9.0`; the
virtualizer and tests target .NET 9. The posts distinguish those targets and
the verified Framework 2.0/4.8.1, Core 3.1, and .NET 9 runs from support claims
for every intermediate runtime. Keep those boundaries current as verification
expands.

## Adding a post

Create a self-contained HTML page under `posts/`, reuse `styles.css`, add a card
to `index.html`, and link to the new article from the related post pages. Keep
results, runtime support, screenshots, and diagrams tied to evidence that
exists. Keep the homepage's development milestones and known limitations in
sync with the README and implementation. Do not describe a suggested or planned
visual as a captured result.
