# Portfolio Protection Notes

This project is intentionally published as source-available portfolio work, not as open-source software.

## What companies can evaluate

- Desktop app architecture with WPF and .NET.
- ATS-friendly resume export.
- PDF import parsing.
- Local trial restrictions.
- Online license activation flow.
- Admin/client installer split.
- Supabase-backed license API.
- Render deployment setup.
- Velopack packaging.

## What is protected

- Real secrets are not stored in the repository.
- Generated installers and local build artifacts are ignored.
- Customer CV data is stored locally per Windows user and is not bundled in installers.
- The license forbids reuse, resale, redistribution, and derivative commercial use.

## Realistic limitation

If a repository is public, people can still read and copy code technically. A restrictive license gives you legal ownership protection, but it does not physically prevent copying.

For a real commercial product, keep the production repository private and use this public repository as a portfolio/demo version.

## Recommended public/private split

Public portfolio repo:

- UI screenshots
- architecture overview
- selected source code
- docs
- safe deployment templates
- no secrets
- no production installers

Private commercial repo:

- production signing
- paid licensing logic
- customer management
- real CI/CD secrets
- release automation
- support tooling
- private admin features
