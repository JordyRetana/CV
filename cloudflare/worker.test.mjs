import test from "node:test";
import assert from "node:assert/strict";
import worker from "./worker.js";

const env = {
  GROQ_API_KEY: "test-only",
  LICENSE_API_URL: "https://license.example.test",
  IMPORT_RATE_LIMITER: { limit: async () => ({ success: true }) },
  IMPORT_GLOBAL_LIMITER: { limit: async () => ({ success: true }) }
};

const resume = {
  professionalSummary: "Backend developer with database experience.",
  experience: [],
  projects: [
    { title: "Project A", details: [{ value: "Built a REST API." }] },
    { title: "Project B", details: [{ value: "Automated database tests." }] }
  ],
  education: [],
  skills: [{ value: "REST APIs" }, { value: "SQL" }]
};

const input = {
  licenseKey: "test-license",
  deviceHash: "test-device",
  jobDescription: "Build reliable backend services, maintain REST APIs and SQL databases.",
  resume,
  isEnglish: true
};

function request(value = input) {
  return new Request("https://cv-editor-ai.example.test/ai/tailor", {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify(value)
  });
}

test("rejects invalid input before contacting license or Groq", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = () => { throw new Error("unexpected fetch"); };
  try {
    const response = await worker.fetch(request({ ...input, jobDescription: "short" }), env);
    assert.equal(response.status, 400);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("rejects unlicensed devices without calling Groq", async () => {
  const originalFetch = globalThis.fetch;
  let calls = 0;
  globalThis.fetch = async () => { calls++; return new Response(null, { status: 403 }); };
  try {
    const response = await worker.fetch(request(), env);
    assert.equal(response.status, 403);
    assert.equal(calls, 1);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("uses supported model and returns every source project", async () => {
  const originalFetch = globalThis.fetch;
  let groqBody;
  globalThis.fetch = async (url, options) => {
    if (url.endsWith("/licenses/verify")) return Response.json({ isActive: true });
    groqBody = JSON.parse(options.body);
    return Response.json({ choices: [{ message: { content: JSON.stringify({
      professionalSummary: "Backend developer with practical experience designing services, maintaining relational databases, and validating application behavior through automated testing. Works with REST APIs and SQL to deliver maintainable solutions. Has built backend features and contributed to projects where reliability, clear interfaces, and thoughtful data access are important. Focuses on accurate implementation and continuous improvement while collaborating with development teams.",
      projects: [
        { sourceIndex: 0, details: ["Built a REST API.", "Designed API endpoints."] },
        { sourceIndex: 1, details: ["Automated database tests.", "Validated SQL behavior."] }
      ],
      skills: ["REST APIs", "SQL"]
    }) } }] });
  };
  try {
    const response = await worker.fetch(request(), env);
    assert.equal(response.status, 200);
    assert.equal((await response.json()).projects.length, 2);
    assert.equal(groqBody.model, "openai/gpt-oss-120b");
    assert.equal(groqBody.response_format.type, "json_schema");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("scheduled check queries the database-backed health endpoint", async () => {
  const originalFetch = globalThis.fetch;
  let fetchedUrl;
  globalThis.fetch = async url => {
    fetchedUrl = url;
    return Response.json({ status: "ok" });
  };
  try {
    let pending;
    await worker.scheduled({}, env, { waitUntil: promise => { pending = promise; } });
    await pending;
    assert.equal(fetchedUrl, "https://license.example.test/health/database");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

const cvText = `Ana Garcia Lopez\nSan Jose, Costa Rica | +506 8713-8971 | ana@example.com\n
RESUMEN PROFESIONAL\nDesarrolladora de software con experiencia en aplicaciones web y APIs REST.\n
EXPERIENCIA\nExample Software, San Jose\nDesarrolladora | 2022 - 2024\nImplemento APIs REST con C# y PostgreSQL.\n
PROYECTOS\nEditor de CV\nDesarrollo una aplicacion de escritorio para editar curriculums.\nIntegro exportacion a PDF y control de versiones.\n
EDUCACION\nUniversidad Ejemplo\nIngenieria en Sistemas | 2023 - Presente\n
HABILIDADES\nC#, PostgreSQL, APIs REST, Git`;

function importRequest(text = cvText) {
  return new Request("https://cv-editor-ai.example.test/ai/import", {
    method: "POST", headers: { "content-type": "application/json", "cf-connecting-ip": "192.0.2.10" },
    body: JSON.stringify({ text, isEnglish: false })
  });
}

test("free import requires neither a license nor a license API call", async () => {
  const originalFetch = globalThis.fetch;
  let calledUrl;
  globalThis.fetch = async (url, options) => {
    calledUrl = url;
    const payload = JSON.parse(options.body);
    assert.equal(payload.response_format.json_schema.strict, true);
    return Response.json({ choices: [{ message: { content: JSON.stringify({
      fullName: "Ana Garcia Lopez", location: "San Jose, Costa Rica", phone: "+506 8713-8971",
      email: "ana@example.com", portfolio: "", linkedIn: "",
      professionalSummary: "Desarrolladora de software con experiencia en aplicaciones web y APIs REST.",
      experience: [{ company: "Example Software", position: "Desarrolladora", location: "San Jose",
        period: "2022 - 2024", responsibilities: ["Implemento APIs REST con C# y PostgreSQL."] }],
      projects: [{ title: "Editor de CV", role: "", location: "",
        details: ["Desarrollo una aplicacion de escritorio para editar curriculums.", "Integro exportacion a PDF y control de versiones."] }],
      education: [{ institution: "Universidad Ejemplo", degree: "Ingenieria en Sistemas",
        description: "", location: "", period: "2023 - Presente" }],
      skills: ["C#", "PostgreSQL", "APIs REST", "Git"]
    }) } }] });
  };
  try {
    const response = await worker.fetch(importRequest(), env);
    assert.equal(response.status, 200);
    const data = await response.json();
    assert.equal(data.projects[0].details.length, 2);
    assert.equal(calledUrl, "https://api.groq.com/openai/v1/chat/completions");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("free import rejects PDFs without selectable text before charging Groq", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = () => { throw new Error("unexpected fetch"); };
  try {
    assert.equal((await worker.fetch(importRequest("scanned page"), env)).status, 400);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("free import is rate limited before contacting Groq", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = () => { throw new Error("unexpected fetch"); };
  try {
    const limited = { ...env, IMPORT_RATE_LIMITER: { limit: async () => ({ success: false }) } };
    assert.equal((await worker.fetch(importRequest(), limited)).status, 429);
  } finally {
    globalThis.fetch = originalFetch;
  }
});
