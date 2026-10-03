using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SurakshaXR.Infrastructure
{
    [Serializable] public sealed class WorkerRecord
    {
        public string id, worker_code, display_name, site_code, department, preferred_locale;
        public string created_at, updated_at;
    }
    [Serializable] public sealed class AttemptRecord
    {
        public string id, worker_id, module_id, module_version, scenario_id, renderer, mode;
        public string started_at, completed_at, score_json, weak_tags_json, actions_json, question_results_json, content_validation_version;
        public decimal score_total;
        public bool passed, critical_fail;
    }
    [Serializable] public sealed class CertificateRecord
    {
        public string id, worker_id, attempt_id, module_id, module_version, issued_at, refresher_due_at;
        public string signer_id, payload_json, signature_b64, qr_payload;
        public int score;
    }
    [Serializable] public sealed class RefresherRecord
    { public string id, worker_id, module_id, due_at, weak_tags_json, status, completed_attempt_id, created_at, updated_at; }

    public sealed class LocalStore : IDisposable
    {
        private readonly ISqliteConnection db;
        public LocalStore(ISqliteConnection connection)
        { db = connection; try { Migrate(); } catch { db.Dispose(); throw; } }
        public void Dispose() => db.Dispose();
        private static string Json(object value) => JsonConvert.SerializeObject(value);
        private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);
        private static void Uuid(string value)
        { if (!Guid.TryParseExact(value, "D", out _)) throw new ArgumentException("Record ID must be a UUID."); }
        private static void Timestamp(string value)
        { if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) || time.Offset != TimeSpan.Zero || !value.EndsWith("Z", StringComparison.Ordinal)) throw new ArgumentException("UTC record timestamp required."); }

        private void Migrate()
        {
            db.Execute("PRAGMA foreign_keys=ON");
            db.Execute("CREATE TABLE IF NOT EXISTS app_meta (key TEXT PRIMARY KEY, value TEXT NOT NULL)");
            var version = db.Query("SELECT value FROM app_meta WHERE key='schema_version'");
            if (version.Count > 0)
            {
                if (version[0]["value"] != "1") throw new InvalidOperationException("Unsupported database schema; database preserved.");
                return;
            }
            db.Begin();
            try
            {
                foreach (var statement in Migration1) db.Execute(statement);
                db.Execute("INSERT INTO app_meta(key,value) VALUES('schema_version','1')");
                db.Commit();
            }
            catch { db.Rollback(); throw; }
        }
        private static readonly string[] Migration1 = {
            "CREATE TABLE workers (id TEXT PRIMARY KEY, worker_code TEXT NOT NULL UNIQUE, display_name TEXT NOT NULL, site_code TEXT, department TEXT, preferred_locale TEXT NOT NULL, pin_hash TEXT, is_active INTEGER NOT NULL DEFAULT 1, created_at TEXT NOT NULL, updated_at TEXT NOT NULL, sync_state TEXT NOT NULL DEFAULT 'pending', record_json TEXT NOT NULL)",
            "CREATE TABLE attempts (id TEXT PRIMARY KEY, worker_id TEXT NOT NULL REFERENCES workers(id), module_id TEXT NOT NULL, module_version TEXT NOT NULL, scenario_id TEXT NOT NULL, renderer TEXT NOT NULL, mode TEXT NOT NULL, started_at TEXT NOT NULL, completed_at TEXT NOT NULL, score_total REAL NOT NULL, score_json TEXT NOT NULL, passed INTEGER NOT NULL, critical_fail INTEGER NOT NULL, weak_tags_json TEXT NOT NULL, actions_json TEXT NOT NULL, question_results_json TEXT NOT NULL, content_validation_version TEXT NOT NULL, created_at TEXT NOT NULL, sync_state TEXT NOT NULL DEFAULT 'pending', record_json TEXT NOT NULL)",
            "CREATE TABLE certificates (id TEXT PRIMARY KEY, worker_id TEXT NOT NULL REFERENCES workers(id), attempt_id TEXT NOT NULL UNIQUE REFERENCES attempts(id), module_id TEXT NOT NULL, module_version TEXT NOT NULL, score REAL NOT NULL, issued_at TEXT NOT NULL, refresher_due_at TEXT, signer_id TEXT NOT NULL, payload_json TEXT NOT NULL, signature_b64 TEXT NOT NULL, qr_payload TEXT NOT NULL, sync_state TEXT NOT NULL DEFAULT 'pending', record_json TEXT NOT NULL)",
            "CREATE TABLE module_progress (id TEXT PRIMARY KEY, worker_id TEXT NOT NULL REFERENCES workers(id), module_id TEXT NOT NULL, module_version TEXT NOT NULL, practice_completed INTEGER NOT NULL DEFAULT 0, latest_attempt_id TEXT REFERENCES attempts(id), best_score REAL, status TEXT NOT NULL, refresher_due_at TEXT, updated_at TEXT NOT NULL, UNIQUE(worker_id,module_id,module_version))",
            "CREATE TABLE refreshers (id TEXT PRIMARY KEY, worker_id TEXT NOT NULL REFERENCES workers(id), module_id TEXT NOT NULL, due_at TEXT NOT NULL, weak_tags_json TEXT NOT NULL, status TEXT NOT NULL, completed_attempt_id TEXT REFERENCES attempts(id), created_at TEXT NOT NULL, updated_at TEXT NOT NULL, record_json TEXT NOT NULL)",
            "CREATE TABLE outbox_events (id TEXT PRIMARY KEY, entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, operation TEXT NOT NULL, payload_json TEXT NOT NULL, created_at TEXT NOT NULL, attempt_count INTEGER NOT NULL DEFAULT 0, last_error TEXT, synced_at TEXT)",
            "CREATE TABLE trusted_signers (signer_id TEXT PRIMARY KEY, display_name TEXT NOT NULL, public_key_b64 TEXT NOT NULL, valid_from TEXT NOT NULL, valid_to TEXT, trust_bundle_version INTEGER NOT NULL, revoked INTEGER NOT NULL)",
            "CREATE TABLE completion_receipts (attempt_id TEXT PRIMARY KEY REFERENCES attempts(id), payload_json TEXT NOT NULL)",
            "CREATE TRIGGER attempts_no_update BEFORE UPDATE ON attempts BEGIN SELECT RAISE(ABORT,'Attempts are append-only'); END",
            "CREATE TRIGGER attempts_no_delete BEFORE DELETE ON attempts BEGIN SELECT RAISE(ABORT,'Attempts are append-only'); END",
            "CREATE TRIGGER certificates_no_update BEFORE UPDATE ON certificates BEGIN SELECT RAISE(ABORT,'Certificates are append-only'); END",
            "CREATE TRIGGER certificates_no_delete BEFORE DELETE ON certificates BEGIN SELECT RAISE(ABORT,'Certificates are append-only'); END"
        };

        public IReadOnlyList<WorkerRecord> Workers() => db.Query("SELECT record_json FROM workers ORDER BY worker_code").Select(x => JsonConvert.DeserializeObject<WorkerRecord>(x["record_json"])).ToArray();
        public IReadOnlyList<AttemptRecord> Attempts(string workerId) => db.Query("SELECT record_json FROM attempts WHERE worker_id=? ORDER BY completed_at DESC,id", workerId).Select(x => JsonConvert.DeserializeObject<AttemptRecord>(x["record_json"])).ToArray();
        public IReadOnlyList<CertificateRecord> Certificates(string workerId) => db.Query("SELECT record_json FROM certificates WHERE worker_id=? ORDER BY issued_at DESC,id", workerId).Select(x => JsonConvert.DeserializeObject<CertificateRecord>(x["record_json"])).ToArray();
        public bool HasCompletedPractice(string workerId, string moduleId, string version) => db.Query("SELECT id FROM module_progress WHERE worker_id=? AND module_id=? AND module_version=? AND practice_completed=1", workerId, moduleId, version).Count != 0;
        public IReadOnlyList<RefresherRecord> Refreshers(string workerId) => db.Query("SELECT record_json FROM refreshers WHERE worker_id=? ORDER BY due_at,id", workerId).Select(x => JsonConvert.DeserializeObject<RefresherRecord>(x["record_json"])).ToArray();
        public IReadOnlyList<Dictionary<string, string>> PendingEvents(int limit = 100)
        {
            if (limit < 1 || limit > 100) throw new ArgumentOutOfRangeException(nameof(limit));
            return db.Query("SELECT * FROM outbox_events WHERE synced_at IS NULL ORDER BY created_at,id LIMIT ?", limit.ToString(CultureInfo.InvariantCulture));
        }
        public void SetPreference(string key, string value)
        {
            if (key != "locale" && key != "active_worker" && key != "reminders" && key != "sounds") throw new ArgumentException("Unsupported preference.");
            db.Execute("INSERT OR REPLACE INTO app_meta(key,value) VALUES(?,?)", key, value);
        }
        public string GetPreference(string key) => db.Query("SELECT value FROM app_meta WHERE key=?", key).FirstOrDefault()?.Values.FirstOrDefault();
        public void AddWorker(WorkerRecord worker)
        {
            Uuid(worker.id); Timestamp(worker.created_at); Timestamp(worker.updated_at);
            if (string.IsNullOrWhiteSpace(worker.worker_code) || string.IsNullOrWhiteSpace(worker.display_name) || !new[] { "en", "hi", "sat" }.Contains(worker.preferred_locale)) throw new ArgumentException("Invalid worker.");
            db.Begin();
            try
            {
                db.Execute("INSERT INTO workers(id,worker_code,display_name,site_code,department,preferred_locale,created_at,updated_at,record_json) VALUES(?,?,?,?,?,?,?,?,?)", worker.id, worker.worker_code, worker.display_name, worker.site_code, worker.department, worker.preferred_locale, worker.created_at, worker.updated_at, Json(worker));
                Enqueue("worker", worker.id, "upsert", Json(worker), worker.created_at);
                db.Commit();
            }
            catch { db.Rollback(); throw; }
        }
        public void SeedWorkers()
        {
            var seeds = new[] {
                new WorkerRecord { id = "7166a5d7-2c09-4cb1-bc8e-43c599f43a21", worker_code = "DEMO-001", display_name = "Demo Worker 1", preferred_locale = "hi" },
                new WorkerRecord { id = "c119c419-7614-48af-ae51-26a73c0a3901", worker_code = "DEMO-002", display_name = "Demo Worker 2", preferred_locale = "sat" },
                new WorkerRecord { id = "b244e6db-16ec-4a8b-8cb8-4134b632f9a0", worker_code = "DEMO-003", display_name = "Demo Worker 3", preferred_locale = "en" }
            };
            foreach (var seed in seeds)
            {
                if (db.Query("SELECT id FROM workers WHERE id=?", seed.id).Count != 0) continue;
                seed.created_at = seed.updated_at = "2026-09-28T00:00:00Z";
                AddWorker(seed);
            }
        }
        public bool Complete(AttemptRecord attempt, CertificateRecord certificate = null, RefresherRecord refresher = null, string completedRefresherId = null)
        {
            Uuid(attempt.id); Uuid(attempt.worker_id); Timestamp(attempt.started_at); Timestamp(attempt.completed_at);
            if (DateTimeOffset.Parse(attempt.completed_at) < DateTimeOffset.Parse(attempt.started_at) || attempt.score_total < 0 || attempt.score_total > 100 || !new[] { "practice", "assessment", "refresher" }.Contains(attempt.mode) || !new[] { "ar", "sim3d" }.Contains(attempt.renderer)) throw new ArgumentException("Invalid attempt.");
            foreach (var text in new[] { attempt.module_id, attempt.module_version, attempt.scenario_id, attempt.content_validation_version })
                if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Attempt content identity required.");
            foreach (var json in new[] { attempt.score_json, attempt.weak_tags_json, attempt.actions_json, attempt.question_results_json }) JToken.Parse(json);
            if (certificate != null)
            {
                Uuid(certificate.id); Timestamp(certificate.issued_at);
                if (!attempt.passed || attempt.mode == "practice" || certificate.attempt_id != attempt.id || certificate.worker_id != attempt.worker_id || certificate.module_id != attempt.module_id || certificate.module_version != attempt.module_version || string.IsNullOrEmpty(certificate.signature_b64) || string.IsNullOrEmpty(certificate.qr_payload)) throw new ArgumentException("Certificate does not match a passed eligible attempt.");
            }
            if (refresher != null) { Uuid(refresher.id); Timestamp(refresher.due_at); if (refresher.worker_id != attempt.worker_id || refresher.module_id != attempt.module_id) throw new ArgumentException("Refresher owner mismatch."); }
            if (completedRefresherId != null) { Uuid(completedRefresherId); if (attempt.mode != "refresher" || !attempt.passed) throw new ArgumentException("Only a passed refresher completes a reminder."); }
            var receiptObject = JObject.FromObject(new { attempt, certificate, refresher });
            if (completedRefresherId != null) receiptObject["completedRefresherId"] = completedRefresherId;
            var receipt = receiptObject.ToString(Formatting.None);
            db.Begin();
            try
            {
                var existing = db.Query("SELECT record_json FROM attempts WHERE id=?", attempt.id);
                if (existing.Count > 0)
                {
                    var receipts = db.Query("SELECT payload_json FROM completion_receipts WHERE attempt_id=?", attempt.id);
                    if (receipts.Count != 1 || !JToken.DeepEquals(JToken.Parse(receipts[0]["payload_json"]), JToken.Parse(receipt))) throw new InvalidOperationException("Append-only completion collision.");
                    db.Commit(); return false;
                }
                db.Execute("INSERT INTO attempts(id,worker_id,module_id,module_version,scenario_id,renderer,mode,started_at,completed_at,score_total,score_json,passed,critical_fail,weak_tags_json,actions_json,question_results_json,content_validation_version,created_at,record_json) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", attempt.id, attempt.worker_id, attempt.module_id, attempt.module_version, attempt.scenario_id, attempt.renderer, attempt.mode, attempt.started_at, attempt.completed_at, Number(attempt.score_total), attempt.score_json, attempt.passed ? "1" : "0", attempt.critical_fail ? "1" : "0", attempt.weak_tags_json, attempt.actions_json, attempt.question_results_json, attempt.content_validation_version, attempt.completed_at, Json(attempt));
                Enqueue("attempt", attempt.id, "append", Json(attempt), attempt.completed_at);
                if (completedRefresherId != null)
                {
                    var source = db.Query("SELECT record_json FROM refreshers WHERE id=? AND worker_id=? AND module_id=? AND status='pending'", completedRefresherId, attempt.worker_id, attempt.module_id).SingleOrDefault();
                    if (source == null) throw new InvalidOperationException("Pending refresher no longer exists; no records saved.");
                    var completed = JsonConvert.DeserializeObject<RefresherRecord>(source["record_json"]);
                    completed.status = "completed"; completed.completed_attempt_id = attempt.id; completed.updated_at = attempt.completed_at;
                    db.Execute("UPDATE refreshers SET status=?,completed_attempt_id=?,updated_at=?,record_json=? WHERE id=?", completed.status, attempt.id, attempt.completed_at, Json(completed), completed.id);
                    Enqueue("refresher", completed.id, "upsert", Json(completed), attempt.completed_at);
                }
                if (certificate != null)
                {
                    db.Execute("INSERT INTO certificates(id,worker_id,attempt_id,module_id,module_version,score,issued_at,refresher_due_at,signer_id,payload_json,signature_b64,qr_payload,record_json) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?)", certificate.id, certificate.worker_id, certificate.attempt_id, certificate.module_id, certificate.module_version, Number(certificate.score), certificate.issued_at, certificate.refresher_due_at, certificate.signer_id, certificate.payload_json, certificate.signature_b64, certificate.qr_payload, Json(certificate));
                    Enqueue("certificate", certificate.id, "append", Json(certificate), attempt.completed_at);
                }
                if (refresher != null)
                {
                    db.Execute("INSERT INTO refreshers(id,worker_id,module_id,due_at,weak_tags_json,status,completed_attempt_id,created_at,updated_at,record_json) VALUES(?,?,?,?,?,?,?,?,?,?)", refresher.id, refresher.worker_id, refresher.module_id, refresher.due_at, refresher.weak_tags_json, refresher.status, refresher.completed_attempt_id, refresher.created_at, refresher.updated_at, Json(refresher));
                    Enqueue("refresher", refresher.id, "upsert", Json(refresher), attempt.completed_at);
                }
                var progress = db.Query("SELECT * FROM module_progress WHERE worker_id=? AND module_id=? AND module_version=?", attempt.worker_id, attempt.module_id, attempt.module_version).FirstOrDefault();
                var practice = attempt.mode == "practice" || (progress != null && progress["practice_completed"] == "1");
                var best = Math.Max(attempt.mode == "practice" ? 0m : attempt.score_total, progress == null || progress["best_score"] == null ? 0m : decimal.Parse(progress["best_score"], CultureInfo.InvariantCulture));
                var status = attempt.mode == "practice" ? (progress?["status"] ?? "IN_PROGRESS") : attempt.passed ? "PASSED" : "NEEDS_RETRAINING";
                // INSERT OR REPLACE is confined to this derived projection, never immutable records.
                db.Execute("INSERT OR REPLACE INTO module_progress(id,worker_id,module_id,module_version,practice_completed,latest_attempt_id,best_score,status,refresher_due_at,updated_at) VALUES(?,?,?,?,?,?,?,?,?,?)", progress?["id"] ?? Guid.NewGuid().ToString("D"), attempt.worker_id, attempt.module_id, attempt.module_version, practice ? "1" : "0", attempt.id, Number(best), status, refresher?.due_at ?? progress?["refresher_due_at"], attempt.completed_at);
                db.Execute("INSERT INTO completion_receipts(attempt_id,payload_json) VALUES(?,?)", attempt.id, receipt);
                db.Commit(); return true;
            }
            catch { db.Rollback(); throw; }
        }
        private void Enqueue(string kind, string entityId, string operation, string payload, string time)
        { db.Execute("INSERT INTO outbox_events(id,entity_type,entity_id,operation,payload_json,created_at) VALUES(?,?,?,?,?,?)", Guid.NewGuid().ToString("D"), kind, entityId, operation, payload, time); }
        public void MarkDelivered(string eventId, string deliveredAt)
        { Uuid(eventId); Timestamp(deliveredAt); db.Execute("UPDATE outbox_events SET synced_at=?,last_error=NULL WHERE id=? AND synced_at IS NULL", deliveredAt, eventId); }
        public void RecordDeliveryFailure(string eventId, string safeErrorCode)
        {
            Uuid(eventId);
            if (!new[] { "UNAVAILABLE", "REJECTED", "INVALID_RESPONSE" }.Contains(safeErrorCode)) throw new ArgumentException("Use a non-sensitive error code.");
            db.Execute("UPDATE outbox_events SET attempt_count=attempt_count+1,last_error=? WHERE id=? AND synced_at IS NULL", safeErrorCode, eventId);
        }
    }
}
