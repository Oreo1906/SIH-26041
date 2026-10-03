SurakshaXR Internal Architecture and Workflow

The main idea
SurakshaXR is a Unity Android application that teaches two scenario-based modules: Fire and Explosion
Response, and Gas Leak and Confined Space Protocol. A worker chooses a module, follows a sequence of
decisions, receives feedback and can take an assessment. The same training content can appear over the
camera in Ground AR, inside a virtual mine driven by the phone's tracked movement, or in a
joystick-controlled 3D simulator.
The most useful way to understand the app is to separate three things. Content describes the scenario and
its rules. The training engine decides what an action means and which step follows. Presentation draws
the scene, shows instructions and plays effects. An extinguisher model does not award marks; an accepted
action in the engine does.
Most worker-side information stays on the phone. Bundled JSON files provide content, local string tables
provide language, SQLite stores records, and local cryptography signs and verifies certificate QR payloads.
The worker flow does not call a server to decide an answer or calculate a score.

<img width="838" height="246" alt="image" src="https://github.com/user-attachments/assets/a7a0366e-d533-4e26-b0b0-f44811fdbf85" />

One concrete example
When a worker selects the configured extinguisher action, the UI sends its stable action ID to
ScenarioRuntime. The runtime records the action, changes its score and step, and returns the result.
TrainingActionEffects then animates the extinguisher and fire. TrainingSessionService eventually packages
the completed session for LocalStore. This ownership chain is the basis of the whole application.
The file references at the end let you move from each explanation to the exact implementation. [S01-S06]

The mobile technology stack
Unity runs the Android application, manages the scene and renders its UI. Most app logic is written in C#.
Small Java bridges give that code access to Android facilities such as SQLite, Keystore and notifications.
These are the project's pinned package versions, rather than a list of newer alternatives.

<img width="842" height="756" alt="image" src="https://github.com/user-attachments/assets/6117272d-d8d8-4fe9-ae58-51fd51f57ea7" />

There is no separately pinned SQLite engine in the APK. Android supplies SQLiteDatabase; desktop storage
code uses a native SQLite interface. JNI is the bridge between Unity C# and the Android Java classes.
The graphics pipeline uses authored meshes, Standard materials and custom fire, smoke, guidance and rock
shaders. It does not use a Universal Render Pipeline package or download 3D content while training. [S01,
S07, S08]

Supporting tools and project assets
The repository also contains Python backend code and a React web application. They are separate
processes from the Android app. Their dependency files belong to their own folders; they are not
automatically packaged into the phone APK

<img width="837" height="752" alt="image" src="https://github.com/user-attachments/assets/840e97b6-1967-4bfe-ae8d-434aaa22aea9" />

Fonts and graphics
Bundled Noto Sans, Devanagari and Ol Chiki fonts render text. Material icons retain Apache 2.0 attribution.
TrainingGeometry creates equipment and mine meshes; shaders control their appearance. Geometry, icons
and fonts are local resources.
Dependency locks contain the full inventory: Unity packages-lock.json, Python requirements files and the
web package-lock.json. [S01, S07, S10, S12]

How the architecture fits together
The Android application has a presentation layer, an application-service layer, a domain layer, infrastructure
and security helpers. The diagram shows responsibility and information flow; it is not a map of separate
Android processes.

<img width="863" height="525" alt="image" src="https://github.com/user-attachments/assets/077ebaa6-430b-40d8-9a7f-d14f06640304" />

Why these boundaries matter
PreviewApp coordinates screens and calls the session service. TrainingSessionService owns the complete
attempt, including the knowledge quiz and save operation. ScenarioRuntime owns the active scenario step
and scoring changes. SimulatorView consumes the selected step's visual commands and places the
corresponding objects.
LocalStore performs database work through ISqliteConnection. Security code creates canonical certificate
bytes, signs them and verifies QR envelopes. These components can be used without an AR camera;
changing the renderer does not require a second scoring implementation.
The React and FastAPI code sit outside this Android boundary. The web UI calls a health endpoint. Local
outbox records are not an active network connection to that endpoint. [S02-S08, S10]

How automatic Ground AR is placed
AR Foundation is the Unity-facing API. ARCore supplies the phone's native tracking and detected planes.
OptionalArSession starts these services only after AR entry and camera permission; it creates ARSession,
XROrigin, the tracked camera and the plane, raycast and anchor managers. AR services must already be
installed: this entry path checks availability without downloading them

<img width="855" height="477" alt="image" src="https://github.com/user-attachments/assets/5a09b10b-ffe3-4ee3-b532-f074f7d08e74" />

Terms used in the guide
Anchor: an AR-tracked reference transform. Prefab: a reusable Unity object asset; much of this app instead
creates objects at runtime. JNI: the C# to Android Java bridge. IL2CPP: Unity's managed-to-native build
pipeline. Canonicalization: producing the same bytes from the same certificate data. Idempotent save:
repeating the same completion does not create another record. Outbox: durable local events waiting for a
sender.




