# MirrorVM Field Notes

This folder is the source for the project's separate static technical blog.
Open `index.html` in a browser to read it. The pages have no build step or
external dependencies; a small local script copies the current page URL when a
reader selects the copy-link button.

The parent `docs/index.html` redirects to this folder. The `docs` directory is
ready to use as a GitHub Pages publishing source; Pages still needs to be
enabled and configured in the repository settings.

The top navigation links to Virtualization, Compatibility, and Architecture.
The project notes list the M1 and M2 posts, compatibility evidence, and
architecture. Milestone post titles match their README milestone names.

The compatibility goal spans .NET Framework 2.0 through 4.8.1 and modern .NET
through 10. The runtime targets `net20`, `netstandard2.0`, and `net9.0`; the
virtualizer and tests target .NET 9. The protected sample and numeric CLR
differential checks passed all 26 configured matrix entries in both x86 and x64
processes. Framework 4.x uses the installed 4.8.1 in-place CLR, so those entries
do not prove separate historical 4.x runtime behavior. Keep this boundary clear
in all pages.

## Adding a post

Create a self-contained HTML page under `posts/`, reuse `styles.css`, add a card
to `index.html`, and link to the new article from the related post pages. For a
milestone post, use the exact milestone name in its page title and project-note
card.
Keep results, runtime support, screenshots, and diagrams tied to evidence that
exists. Keep the homepage's development milestones and known limitations in
sync with the README and implementation. Do not describe a suggested or planned
visual as a captured result.
