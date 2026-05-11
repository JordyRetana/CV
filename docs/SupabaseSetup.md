# Supabase setup

No guardes la contrasena real de Supabase dentro del repositorio.

## Variables necesarias

Configura esta variable como secreto en tu maquina, servidor o GitHub Actions:

```text
SUPABASE_CONNECTION_STRING=postgresql://USER:PASSWORD@HOST:PORT/DATABASE
```

## Tablas recomendadas

```sql
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
  created_at timestamptz not null default now()
);

create table if not exists devices (
  id uuid primary key default gen_random_uuid(),
  license_id uuid references licenses(id),
  device_hash text not null,
  first_seen_at timestamptz not null default now(),
  last_seen_at timestamptz not null default now(),
  unique (license_id, device_hash)
);

create table if not exists activations (
  id uuid primary key default gen_random_uuid(),
  license_id uuid references licenses(id),
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
```

## Importante

La app WPF no debe conectarse directo a Supabase con usuario administrador.
Debe hablar con una API propia que valide licencias. La API es la que usa Supabase.
