using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SurakshaXR.Infrastructure;

namespace SurakshaXR.Tests
{
    public sealed class PersistenceTests
    {
        private string path;
        private LocalStore store;
        private ISqliteConnection connection;
        [SetUp] public void Open()
        {
            path = Path.Combine(Path.GetTempPath(), "surakshaxr-test-" + Guid.NewGuid() + ".db");
            connection = SqliteConnection.Open(path);
            store = new LocalStore(connection);
            store.SeedWorkers();
        }
        [TearDown] public void Close() { store?.Dispose(); if (File.Exists(path)) File.Delete(path); }
        private AttemptRecord Attempt(string mode = "assessment") => new AttemptRecord {
            id = Guid.NewGuid().ToString("D"), worker_id = store.Workers()[0].id,
            module_id = "fire", module_version = "1.0.0", scenario_id = "fire_v1", renderer = "sim3d", mode = mode,
            started_at = "2026-09-29T09:00:00Z", completed_at = "2026-09-29T09:10:00Z",
            score_total = 85, passed = true, critical_fail = false, content_validation_version = "demo-unvalidated-v1",
            score_json = "{\"procedure\":85}", actions_json = "[]", weak_tags_json = "[]", question_results_json = "[]"
        };
        private CertificateRecord Certificate(AttemptRecord attempt) => new CertificateRecord {
            id = Guid.NewGuid().ToString("D"), worker_id = attempt.worker_id, attempt_id = attempt.id,
            module_id = attempt.module_id, module_version = attempt.module_version, issued_at = attempt.completed_at,
            score = 85, signer_id = "test-only", payload_json = "{}", signature_b64 = "test-fixture-not-a-signature", qr_payload = "test-fixture-only"
        };
        [Test] public void ReloadRetainsUnicodeProfilesPreferencesAndAttempts()
        {
            var worker = new WorkerRecord { id = Guid.NewGuid().ToString("D"), worker_code = "LOCAL-004", display_name = "प्रशिक्षण ᱥᱟᱱᱛᱟᱲᱤ", preferred_locale = "sat", created_at = "2026-09-29T00:00:00Z", updated_at = "2026-09-29T00:00:00Z" };
            store.AddWorker(worker); store.SetPreference("locale", "hi"); store.SetPreference("locale", "sat");
            store.SetPreference("active_worker", worker.id);
            var attempt = Attempt(); attempt.worker_id = worker.id;
            store.Complete(attempt, Certificate(attempt));
            var eventId = store.PendingEvents()[0]["id"];
            store.Dispose(); connection = SqliteConnection.Open(path); store = new LocalStore(connection); store.SeedWorkers();
            Assert.That(store.Workers().Count, Is.EqualTo(4));
            Assert.That(store.Workers()[3].display_name, Is.EqualTo(worker.display_name));
            Assert.That(store.GetPreference("locale"), Is.EqualTo("sat"));
            Assert.That(store.GetPreference("active_worker"), Is.EqualTo(worker.id));
            Assert.That(store.Attempts(worker.id)[0].id, Is.EqualTo(attempt.id));
            Assert.That(store.Certificates(worker.id).Count, Is.EqualTo(1));
            Assert.That(store.PendingEvents()[0]["id"], Is.EqualTo(eventId));
        }
        [Test] public void SoundTogglePersistsBothStatesWithoutAllowingUnknownMetadataWrites()
        {
            Assert.That(store.GetPreference("sounds"), Is.Null);
            var pending = store.PendingEvents().Count;
            foreach (var state in new[] { "disabled", "enabled" }) {
                store.SetPreference("sounds", state);
                Assert.Throws<ArgumentException>(() => store.SetPreference("unknown_setting", "enabled"));
                Assert.Throws<ArgumentException>(() => store.SetPreference("schema_version", "999"));
                store.Dispose(); connection = SqliteConnection.Open(path); store = new LocalStore(connection);
                Assert.That(store.GetPreference("sounds"), Is.EqualTo(state));
                Assert.That(store.GetPreference("unknown_setting"), Is.Null);
                Assert.That(store.GetPreference("schema_version"), Is.EqualTo("1"));
                Assert.That(store.PendingEvents().Count, Is.EqualTo(pending));
            }
        }
        [Test] public void CompletionReplayIsIdempotentAndChangedPayloadIsRejected()
        {
            var attempt = Attempt(); var cert = Certificate(attempt);
            Assert.That(store.Complete(attempt, cert), Is.True);
            var count = store.PendingEvents().Count;
            Assert.That(store.Complete(attempt, cert), Is.False);
            Assert.That(store.PendingEvents().Count, Is.EqualTo(count));
            attempt.score_total = 90;
            Assert.Throws<InvalidOperationException>(() => store.Complete(attempt, cert));
            Assert.That(store.Attempts(attempt.worker_id)[0].score_total, Is.EqualTo(85));
        }
        [Test] public void ChangedRefresherReplayCannotSilentlyReplaceCompletion()
        {
            var attempt = Attempt();
            var reminder = new RefresherRecord { id = Guid.NewGuid().ToString("D"), worker_id = attempt.worker_id, module_id = attempt.module_id, due_at = "2026-10-29T00:00:00Z", weak_tags_json = "[]", status = "pending", created_at = attempt.completed_at, updated_at = attempt.completed_at };
            store.Complete(attempt, null, reminder);
            Assert.That(store.Complete(attempt, null, reminder), Is.False);
            reminder.due_at = "2026-11-29T00:00:00Z";
            Assert.Throws<InvalidOperationException>(() => store.Complete(attempt, null, reminder));
        }
        [Test] public void DatabaseEnforcesAppendOnlyRecordsAndForeignKeys()
        {
            var attempt = Attempt(); var cert = Certificate(attempt); store.Complete(attempt, cert);
            Assert.Throws<InvalidOperationException>(() => connection.Execute("UPDATE attempts SET score_total=100 WHERE id=?", attempt.id));
            Assert.Throws<InvalidOperationException>(() => connection.Execute("DELETE FROM certificates WHERE id=?", cert.id));
            var missingWorker = Attempt(); missingWorker.worker_id = Guid.NewGuid().ToString("D");
            Assert.Throws<InvalidOperationException>(() => store.Complete(missingWorker));
            Assert.That(store.Attempts(attempt.worker_id).Count, Is.EqualTo(1));
        }
        [Test] public void InjectedOutboxFailureRollsBackAttemptAndDerivedProgress()
        {
            var failing = new FailOutbox(connection);
            using (var unit = new LocalStore(failing))
            {
                var attempt = Attempt(); failing.Fail = true;
                Assert.Throws<IOException>(() => unit.Complete(attempt));
                Assert.That(unit.Attempts(attempt.worker_id), Is.Empty);
                Assert.That(connection.Query("SELECT * FROM module_progress"), Is.Empty);
                Assert.That(unit.PendingEvents().Count, Is.EqualTo(3));
                failing.Fail = false;
                Assert.That(unit.Complete(attempt), Is.True);
            }
        }
        [Test] public void DeliveryMetadataDoesNotMutateAttemptAndPracticeDoesNotInflateBestScore()
        {
            var assessment = Attempt(); store.Complete(assessment);
            var practice = Attempt("practice"); practice.score_total = 100; store.Complete(practice);
            Assert.That(connection.Query("SELECT best_score FROM module_progress")[0]["best_score"], Is.EqualTo("85.0"));
            var queued = store.PendingEvents()[0]["id"];
            store.RecordDeliveryFailure(queued, "UNAVAILABLE");
            store.MarkDelivered(queued, "2026-09-29T10:00:00Z");
            store.MarkDelivered(queued, "2026-09-29T10:10:00Z");
            Assert.That(connection.Query("SELECT synced_at FROM outbox_events WHERE id=?", queued)[0]["synced_at"], Is.EqualTo("2026-09-29T10:00:00Z"));
            Assert.That(store.Attempts(assessment.worker_id).Count, Is.EqualTo(2));
        }
        [Test] public void FutureSchemaIsRejectedWithoutErasingDatabase()
        {
            connection.Execute("UPDATE app_meta SET value='999' WHERE key='schema_version'");
            store.Dispose(); store = null;
            Assert.Throws<InvalidOperationException>(() => new LocalStore(SqliteConnection.Open(path)));
            using (var check = SqliteConnection.Open(path)) Assert.That(check.Query("SELECT id FROM workers").Count, Is.EqualTo(3));
        }
        private sealed class FailOutbox : ISqliteConnection
        {
            private readonly ISqliteConnection inner; public bool Fail;
            public FailOutbox(ISqliteConnection connection) { inner = connection; }
            public void Execute(string sql, params string[] values) { if (Fail && sql.StartsWith("INSERT INTO outbox_events", StringComparison.Ordinal)) throw new IOException("Injected failure"); inner.Execute(sql, values); }
            public List<Dictionary<string, string>> Query(string sql, params string[] values) => inner.Query(sql, values);
            public void Begin() => inner.Begin(); public void Commit() => inner.Commit(); public void Rollback() => inner.Rollback();
            public void Dispose() { /* Outer test owns the connection. */ }
        }
    }
}
