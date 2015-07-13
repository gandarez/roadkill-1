Contributing
==
There are various ways you can contribute to Roadkill:

Improve the documentation
---

You'll need to be familiar with [Markdown](https://help.github.com/articles/github-flavored-markdown/) to do this, the documentation is held as a set of .md files in source control, under /docs/source. MarkdownPad is a great free tool for editing the files.

Create a new theme
---

See the previous Themes sections on how to do this.

Find bugs
---

..and report them via Bitbucket. But bare in mind the project has some fairly comprehensive automated test coverage (over 500 tests), so a bug hunt is only likely to discover a few unknown issues.

Suggest new features
---

Please do do this via [UserEcho](http://roadkillwiki.userecho.com/)

Write some code
---

Contribute a patch or have a chat on the Google groups about adding a new feature.

The project doesn't use Codeplex team memberships for commits - we use the brave new world of DCVS where you can clone the repository, make some changes, and then push your changes to our repository (a 'pull request'). We can then diff the changes, and accept or deny them.

If you want to write some code, please stick to the following guidelines for your contributions:

- Use the existing style of code found in the source base - new lines for curlies, '_' prefix for private members, plus the standard .NET framework guidelines on naming.
- Tabs for indentation (4 spaces per tab).
- If you do have a different coding style to this don't worry, the code is likely to be auto-reformated anyway (using Visual Studio's built in shortcut key). Don't take this personally if it happens - it's about consistency rather than my-style-is-better-than-yours.
- Use XML documentation for public classes, properties and methods, and where needed private methods too.
- No `var`s! I get religious about `var` abuse, in my view it makes code harder to read and unless used for its [intended purpose](http://blogs.msdn.com/b/ericlippert/archive/2011/04/20/uses-and-misuses-of-implicit-typing.aspx) - where the left side is redundant, dynamic LINQ or complex generic declarations (Dictionary of T for example), then please don't pepper the code with them.
- Add unit tests for the code where possible, which can be a unit test, integration test or acceptance (Selenium-based) test.
- Use constructor injection for dependencies, and add these into the DependencyContainer class (this makes testing and mocking a lot easier).
- Uncle Bob's approach to coding and KISS is the most important factor for the project, it should be easy to read instantly.
