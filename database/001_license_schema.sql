create extension if not exists pgcrypto;

create table if not exists app_users (
  id uuid primary key default gen_random_uuid(),
  email text unique not null,
  full_name text,
  is_active boolean not null default true,
  created_at timestamptz not null default now()
);

create table if not exists licenses (
  id uuid primary key default gen_random_uuid(),
  user_id uuid references app_users(id),
  license_key_hash text unique not null,
  status text not null default 'active',
  kind text not null default 'premium',
  max_devices integer not null default 1,
  expires_at timestamptz,
  created_at timestamptz not null default now(),
  constraint licenses_status_check check (status in ('active', 'inactive', 'revoked')),
  constraint licenses_kind_check check (kind in ('trial', 'premium', 'developer'))
);

create table if not exists devices (
  id uuid primary key default gen_random_uuid(),
  license_id uuid not null references licenses(id) on delete cascade,
  device_hash text not null,
  first_seen_at timestamptz not null default now(),
  last_seen_at timestamptz not null default now(),
  unique (license_id, device_hash)
);

create table if not exists activations (
  id uuid primary key default gen_random_uuid(),
  license_id uuid references licenses(id) on delete set null,
  device_hash text not null,
  app_version text not null,
  status text not null,
  ip_address text,
  created_at timestamptz not null default now()
);

create table if not exists audit_logs (
  id uuid primary key default gen_random_uuid(),
  actor text not null,
  action text not null,
  entity_type text not null,
  entity_id text,
  metadata jsonb not null default '{}'::jsonb,
  created_at timestamptz not null default now()
);

create index if not exists idx_licenses_user_id on licenses(user_id);
create index if not exists idx_devices_license_id on devices(license_id);
create index if not exists idx_activations_license_id on activations(license_id);
create index if not exists idx_activations_created_at on activations(created_at desc);
