# Contributing to Maintenance OS

First off, thank you for considering contributing to Maintenance OS! It's people like you that make Maintenance OS such a great tool for the community.

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

---

## 📋 Ways to Contribute

There are many ways you can contribute, and not all of them require writing code:
- **Reporting Bugs**: Let us know if something isn't working right.
- **Suggesting Features**: Have an idea for a new maintenance task or UI effect?
- **Documentation**: Improve the README, add comments, or write guides.
- **Code**: Submit a fix or a new feature via a PR.
- **Design**: Help us refine the cyberpunk aesthetics.
- **Triage**: Help manage issues and pull requests.

---

## 🐛 Reporting Bugs

Before creating bug reports, please check our [issue tracker](https://github.com/Beardicuss/Maintenance-OS/issues) to see if the problem has already been reported.

When reporting a bug, please include:
- A clear, descriptive title.
- Steps to reproduce the bug.
- Expected vs. actual behavior.
- Your environment (Windows version, .NET version).
- Any relevant logs or screenshots.

Please use our [Bug Report Template](ISSUE_TEMPLATE/bug_report.md).

---

## 💡 Suggesting Features

We are always looking for new ideas! If you have a feature request:
- Check if it's already in our [Roadmap](README.md#roadmap).
- Open an issue using the [Feature Request Template](ISSUE_TEMPLATE/feature_request.md).
- Provide as much detail as possible about the motivation and use case.

---

## 🛠️ Development Setup

### 1. Fork & Clone
Fork the repository on GitHub and clone it locally:
```bash
git clone https://github.com/Beardicuss/Maintenance-OS.git
cd Maintenance-OS
```

### 2. Prerequisites
Ensure you have the following installed:
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [VS Code](https://code.visualstudio.com/).
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

### 3. Install Dependencies
Open the solution file or run:
```bash
dotnet restore
```

### 4. Running Locally
To run the screensaver in demo mode:
```bash
dotnet run --project src/SoftcurseLab.csproj -- /s
```

### 5. Running Tests
```bash
dotnet test
```

---

## ⌨️ Making Changes

### Branch Naming Convention
Use descriptive branch names:
- `feat/user-auth`
- `fix/login-crash`
- `docs/api-reference`
- `style/neon-glow`

### Commit Message Convention
We follow [Conventional Commits](https://www.conventionalcommits.org/):
- `feat:`: A new feature
- `fix:`: A bug fix
- `docs:`: Documentation only changes
- `style:`: Changes that do not affect the meaning of the code (white-space, formatting, etc.)
- `refactor:`: A code change that neither fixes a bug nor adds a feature
- `perf:`: A code change that improves performance
- `test:`: Adding missing tests or correcting existing tests
- `chore:`: Updates to the build process or auxiliary tools

### Linting and Formatting
Before committing, ensure your code follows the project's style:
```bash
dotnet format
```

---

## 🚀 Submitting a Pull Request

1. Create a new branch.
2. Make your changes and commit them with descriptive messages.
3. Push your branch to your fork.
4. Open a Pull Request against the `main` branch.
5. Fill out the [PR Template](PULL_REQUEST_TEMPLATE.md).

### PR Checklist
- [ ] Tests pass.
- [ ] Linting/formattng checks pass.
- [ ] Documentation is updated.
- [ ] You have performed a self-review.

---

## 📦 Release Process

Maintainers cut releases by:
1. Updating the version in `SoftcurseLab.csproj`.
2. Updating `CHANGELOG.md`.
3. Tagging a new release on GitHub.
4. Automated CI/CD will handle the build and attachment of `.scr` binaries.

---

## 💬 Getting Help

If you have questions, please use [GitHub Discussions](https://github.com/Beardicuss/Maintenance-OS/discussions) or reach out via **security@beardicuss.com** for sensitive matters.

Thank you for contributing! 🚀
