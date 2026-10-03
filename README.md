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
