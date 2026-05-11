# Security Policy

## Reporting Security Issues

If you find a vulnerability or exposed secret, do not open a public issue.

Contact: jretanamendez@gmail.com

## Secrets

This repository must not contain real production secrets.

Examples of secrets that must stay outside git:

- Supabase passwords and connection strings with real credentials
- GitHub personal access tokens
- Render environment values
- `ADMIN_API_KEY`
- signing certificates
- private keys
- customer license keys
- customer CV files or exported resumes

Use environment variables, Render secrets, GitHub Actions secrets, or local `.env` files ignored by git.

## Public Portfolio Mode

This repository is source-available for portfolio review. It is not open source.

The visible code demonstrates architecture and implementation skill, but commercial deployment should use:

- rotated production credentials;
- private CI/CD secrets;
- signed desktop installers;
- a private production repository or protected release pipeline;
- server-side license validation;
- monitored backend logs;
- regular dependency updates.

## If A Secret Is Accidentally Committed

1. Rotate the secret immediately in the provider dashboard.
2. Remove it from the current tree.
3. Treat the old value as compromised.
4. Rewrite git history only if necessary and after coordinating with anyone who cloned the repo.
