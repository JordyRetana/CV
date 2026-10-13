import test from "node:test";
import assert from "node:assert/strict";
import worker from "./worker.js";

const env = {
  GROQ_API_KEY: "test-only",
  LICENSE_API_URL: "https://license.example.test"
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
