# Security Policy

## Supported Versions

We are committed to providing security updates for the current major version of Maintenance OS.

| Version | Supported |
|---------|-----------|
| 1.x     | ✅         |
| < 1.0   | ❌         |

## Reporting a Vulnerability

**Do NOT open a public GitHub issue for security vulnerabilities.**

If you discover a potential security vulnerability in this project, please report it to us through one of the following methods:

- **Email**: security@beardicuss.com
- **Private Vulnerability Reporting**: Use the "Report a vulnerability" button on the GitHub repository security tab.

### What to include in the report:
- A clear description of the vulnerability.
- Proof-of-concept (PoC) code, screenshots, or detailed reproduction steps.
- Assessment of the potential impact.
- Any suggested mitigations.

## Response Timeline

- **Acknowledgement**: Within 48 hours.
- **Initial assessment**: Within 7 days.
- **Patch or mitigation**: Within 90 days of confirmation (often much faster).
- **Public disclosure**: Coordinated after a patch has been released and users have had time to update.

## Security Advisories

Security advisories will be published via [GitHub Security Advisories](https://github.com/Beardicuss/Maintenance-OS/security/advisories).

## Out of Scope

The following are generally considered out of scope for our security policy:
- Social engineering attacks against maintainers.
- Denial of Service (DoS) attacks on free-tier hosting infrastructure.
- Issues in versions that are no longer supported (EOL).
- Theoretical vulnerabilities without a practical PoC.

## Bug Bounty

Currently, Maintenance OS does not have a formal bug bounty program. We are a community-driven project and appreciate your voluntary assistance in keeping the project secure.

## Security Best Practices for Contributors

- **No Secrets**: Never commit API keys, passwords, or other sensitive data.
- **Dependency Pinning**: Use fixed versions for critical dependencies where possible.
- **Sanitize Input**: Always validate and sanitize data from external processes or files.
- **Sensitive Data in PRs**: If a PR requires discussing sensitive data, contact us via email first.
