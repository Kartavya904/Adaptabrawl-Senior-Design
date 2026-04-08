<div align="center">

# 🎮 Adaptabrawl

### _A 2D Multiplayer Fighting Game with Adaptive Combat_

[![Unity](https://img.shields.io/badge/Unity-6000.2.6f2-black?style=for-the-badge&logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)
[![Status](https://img.shields.io/badge/Status-Spring%202026%20Final-blue?style=for-the-badge)]()

**Senior Design Project | University of Cincinnati | Fall 2025 - Spring 2026**

[⬇️ Download build](#-download-playable-build-windows) • [📹 Watch Demo](#-demo-video) • [🎪 Expo demo](#spring-2026-expo-demo) • [🚀 Quick Start](#-quick-start) • [📚 Documentation](#-table-of-contents) • [👥 Team](#-team)

---

</div>

## 📋 Project Abstract

**Adaptabrawl** is a 2D multiplayer fighting game that blends attack, defense, and evasion into a fast, legible combat loop. Matches evolve through adaptive conditions—stage/weather/match modifiers—and clear status effects such as poison, stagger/heavy-attack windup, and low-HP state. The experience prioritizes readability (icons, timers, telegraphs) and fairness, ensuring competitive gameplay with accessible mechanics.

**Course submission:** [Final Design Report (Spring 2026)](Homework%20Deliverables/Spring%202026/07%20-%20Final%20Design%20Report/Adaptabrawl_Final_Design_Report.md)

---

## ⬇️ Download playable build (Windows)

**Direct download (zip):** [Adaptabrawl-Windows.zip](https://github.com/Kartavya904/Adaptabrawl-Senior-Design/releases/latest/download/Adaptabrawl-Windows.zip)

**All releases:** [GitHub Releases](https://github.com/Kartavya904/Adaptabrawl-Senior-Design/releases/latest)

Unpack the zip, then run `Adaptabrawl.exe`. Keep the `Adaptabrawl_Data` folder next to the executable.

> **Maintainers:** Publish a Release on GitHub and attach a zip named `Adaptabrawl-Windows.zip` (or edit this README link to match your release asset filename). The repository also contains a local build tree under [`Adaptabrawl_Builds/`](Adaptabrawl_Builds/) for development; that folder is not a single-file download until zipped and uploaded to Releases.

---

## 👥 Team

| Name               | Major            | Email                | Role                     |
| ------------------ | ---------------- | -------------------- | ------------------------ |
| **Kartavya Singh** | Computer Science | singhk6@mail.uc.edu  | Netcode & Infrastructure |
| **Saarthak Sinha** | Computer Science | sinhas6@mail.uc.edu  | Combat/Systems & Tooling |
| **Kanav Shetty**   | Computer Science | shettykv@mail.uc.edu | UX/UI, VFX/SFX, Stages   |
| **Yash Ballabh**   | Computer Science | ballabyh@mail.uc.edu | Tools, CI/CD & QA        |

**Faculty Advisor:** Vikas Rishishwar | vikas@classmonitor.com

---

## 🗂️ Table of Contents

### 📄 Core Documentation

- [**Download playable build (Windows)**](#-download-playable-build-windows) — GitHub Releases zip
- [**Project Description**](#-project-description) — Comprehensive overview and objectives
- [**User Stories & Design Diagrams**](#-user-stories--design-diagrams) — Requirements and system architecture
- [**Project Tasks & Timeline**](#-project-tasks--timeline) — Development roadmap and effort distribution

### 🎓 Academic Deliverables (Fall 2025)

1. [**Professional Biographies**](#1-professional-biographies) — Team member profiles and experience
2. [**Project Description (Assignment #2)**](#2-project-description-assignment-2) — Detailed project scope
3. [**Team contract & reflections (Assignment #3)**](#3-team-contract--self-assessments-assignment-3) — Team agreement; self-assessment files where committed
4. [**User Stories & Design Diagrams (Assignment #4)**](#4-user-stories--design-diagrams-assignment-4) — Requirements engineering
5. [**Project Tasks & Timeline (Assignments #5-6)**](#5-project-tasks--timeline-assignments-5-6) — Planning and scheduling
6. [**ABET Concerns Essay (Assignment #7)**](#6-abet-concerns-essay-assignment-7) — Professional constraints analysis
7. [**Fall Design Presentation (Assignment #8)**](#7-fall-design-presentation-assignment-8) — Project presentation
8. [**Video Presentation (Assignment #9)**](#8-video-presentation-assignment-9) — Demonstration and walkthrough

### 🌸 Spring 2026 (final semester)

- [**Final Design Report**](#spring-2026-final-deliverables) — Table of contents, hours, expenses, appendix
- [**Test plan**](#spring-2026-final-deliverables) — Execution and test matrix
- [**User manual & FAQ**](#spring-2026-final-deliverables) — Player-facing documentation
- [**Expo presentation, poster & demo video**](#spring-2026-final-deliverables) — PPT, PDF poster, and [`Adaptabrawl_Expo_Demo.mp4`](Miscellaneous/Media/Video/Adaptabrawl_Expo_Demo.mp4)

### 💻 Technical Resources

- [**Tech Stack**](#-tech-stack) — Technologies and frameworks
- [**Quick Start Guide**](#-quick-start) — Installation and setup
- [**Budget**](#-budget) — Financial overview
- [**Appendix**](#-appendix) — References, citations, and meeting notes

---

## 🚀 Quick Start

### Prerequisites

- **Unity Hub** (latest version)
- **Unity LTS 6000.2.6f2** or compatible LTS version
- **Git** for version control
- **Windows** or **macOS** (target platforms)

### Installation

1. **Clone the repository:**

   ```bash
   git clone https://github.com/Kartavya904/Adaptabrawl-Senior-Design.git
   cd Adaptabrawl-Senior-Design
   ```

2. **Open in Unity Hub:**
   - Launch Unity Hub
   - Click **Add** / **Open** and select the Unity project folder:  
     `Adaptabrawl/Adaptabrawl`  
     (the directory that contains `Assets`, `Packages`, and `ProjectSettings`)
   - Unity will load the project (this may take several minutes on first import)

3. **Run the game in the Editor:**
   - Open `Assets/Scenes/StartScene.unity` (or your intended entry scene)
   - Press **Play**

### 🎮 Controls

- **Player 1:** Arrow Keys (Movement) + F/G/R/T (Actions)
- **Player 2:** NumPad (Movement + Actions)

---

## 🛠️ Tech Stack

<div align="center">

| Category                 | Technology                                     |
| ------------------------ | ---------------------------------------------- |
| **Game Engine**          | Unity LTS 6000.2.6f2 (Editor v3.15.1)          |
| **Programming Language** | C# 12.0                                        |
| **Networking**           | Mirror Framework, Unity Netcode + Relay/Lobby  |
| **Version Control**      | Git & GitHub                                   |
| **Target Platforms**     | Windows, macOS                                 |
| **Architecture**         | Data-driven ScriptableObjects, Factory Pattern |

</div>

---

## 📄 Project Description

Adaptabrawl addresses the need for accessible yet competitive fighting games. Our solution combines:

- **Adaptive Combat System:** Dynamic stage conditions, weather effects, and match modifiers that evolve gameplay
- **Clear Visual Feedback:** Icons, timers, and telegraphs for all game states
- **Status Effect System:** Poison, stagger, heavy-attack windup, low-HP states
- **Fair Multiplayer:** Balanced mechanics ensuring competitive integrity
- **Modular Architecture:** ScriptableObject-based design for easy content creation

### Key Features

✅ Two-player local multiplayer  
✅ Character-specific movesets with normal and special attacks  
✅ Ground detection and physics-based movement  
✅ Modular fighter and move definition system  
✅ One-click match setup wizard  
✅ Modern Unity physics (LinearVelocity API)

**[📖 Read Full Project Description](Project-Description.md)**

---

## 📊 User Stories & Design Diagrams

### User Stories

Our design is driven by user-centric requirements:

1. **As a casual player**, I want intuitive controls and clear visual feedback so I can enjoy the game without extensive training
2. **As a competitive player**, I want balanced mechanics and deep strategic options so matches reward skill
3. **As a content creator**, I want diverse characters and stages so I can create engaging content
4. **As a developer**, I want modular systems so I can easily add new fighters and moves

### Design Diagrams

<details>
<summary><b>📐 Level 0 Diagram (System Context)</b></summary>

High-level view showing players interacting with the game system through input devices and receiving audio/visual feedback.

**[View Full Diagram](Homework%20Deliverables/Fall%202025/04%20-%20Design%20Diagrams/Design_Diagrams_Adaptabrawl.pdf)**

</details>

<details>
<summary><b>📐 Level 1 Diagram (Major Components)</b></summary>

Shows major subsystems: Input Management, Game Logic, Rendering Engine, Physics System, and Network Layer.

**[View Full Diagram](Homework%20Deliverables/Fall%202025/04%20-%20Design%20Diagrams/Design_Diagrams_Adaptabrawl.pdf)**

</details>

<details>
<summary><b>📐 Level 2 Diagram (Detailed Architecture)</b></summary>

Detailed component interactions including FighterController, MovementController, InputManager, and ScriptableObject data flow.

**[View Full Diagram](Homework%20Deliverables/Fall%202025/04%20-%20Design%20Diagrams/Design_Diagrams_Adaptabrawl.pdf)**

</details>

### Diagram Conventions

- **Rectangles:** System components/modules
- **Circles/Ovals:** External entities (players, devices)
- **Arrows:** Data flow and dependencies
- **Dashed lines:** Optional or conditional connections

**[📁 View Complete Design Diagrams](Homework%20Deliverables/Fall%202025/04%20-%20Design%20Diagrams/Design_Diagrams_Adaptabrawl.pdf)** · [User stories (Markdown)](Homework%20Deliverables/Fall%202025/04%20-%20Design%20Diagrams/User_Stories.md) · [Diagram source (draw.io)](Homework%20Deliverables/Fall%202025/04%20-%20Design%20Diagrams/Design%20Diagrams/Adaptabrawl_Design_Diagrams.drawio)

---

## 📅 Project Tasks & Timeline

### Task List

Our development is organized into major milestones:

1. **Core Combat System** — Fighter movement, basic attacks, collision detection
2. **Character System** — ScriptableObject architecture, fighter definitions
3. **Input Management** — Two-player input handling, control mapping
4. **Visual Feedback** — Animations, VFX, UI elements
5. **Stage System** — Dynamic environments, modifiers
6. **Network Implementation** — Multiplayer synchronization
7. **Polish & Testing** — Balance tuning, bug fixes, optimization

### Timeline Overview

- **September - October 2025:** Core systems and architecture
- **November 2025:** Character implementation, input refinement
- **December 2025:** Polish, testing, and documentation
- **January - February 2026:** Network implementation (Spring semester)
- **March - April 2026:** Final polish and deployment (Spring semester)

### Effort Matrix

Each team member has contributed **45+ hours** across:

- Requirements analysis and design
- Implementation and coding
- Testing and debugging
- Documentation and presentations
- Team meetings and coordination

**[📊 View Detailed Task List (PDF)](Homework%20Deliverables/Fall%202025/05%20-%20Tasklist/Tasklist_Adaptabrawl.pdf)** · [Task list (Markdown)](Homework%20Deliverables/Fall%202025/05%20-%20Tasklist/Tasklist.md)  
**[📈 View Complete Timeline & Effort Matrix (PDF)](Homework%20Deliverables/Fall%202025/06%20-%20Milestone%2C%20Timeline%2C%20and%20Effort%20Matrix/Assignment_6_Adaptabrawl_Milestones_Timeline_Effort_Matrix.pdf)**

---

## 🎓 Academic Deliverables

### 1. Professional Biographies

Individual biographies detailing team members' education, work experience, project involvement, and areas of expertise.

**[📂 View All Biographies](Homework%20Deliverables/Fall%202025/01%20-%20Professional%20Biography/)**

---

### 2. Project Description (Assignment #2)

Comprehensive project description including problem statement, proposed solution, target audience, and success criteria.

**[📄 View Project Description](Project-Description.md)**

---

### 3. Team Contract & Self-Assessments (Assignment #3)

Team agreement and individual reflections (commit whatever your section submitted).

**[📝 Team contract (PDF)](Homework%20Deliverables/Fall%202025/03%20-%20Team%20Contract/Adaptabrawl_Team_Contract.pdf)** · [Team contract (DOCX)](Homework%20Deliverables/Fall%202025/03%20-%20Team%20Contract/Adaptabrawl_Team_Contract.docx)  
**Spring 2026 final self-assessment (example):** [Final individual self-assessment (Singhk6)](Homework%20Deliverables/Spring%202026/06%20-%20Self%20Assessment/Final_Individual_Self_Assessment_Singhk6.txt)

---

### 4. User Stories & Design Diagrams (Assignment #4)

Complete requirements engineering documentation including:

- User stories from multiple stakeholder perspectives
- Level 0, 1, and 2 design diagrams
- Diagram conventions and component descriptions

**[📐 View Design Diagrams (PDF)](Homework%20Deliverables/Fall%202025/04%20-%20Design%20Diagrams/Design_Diagrams_Adaptabrawl.pdf)**

---

### 5. Project Tasks & Timeline (Assignments #5-6)

Detailed project planning documentation:

- **Task List:** Breakdown of all development tasks
- **Timeline:** Gantt chart showing task dependencies and schedule
- **Effort Matrix:** Hour distribution across team members

**[✅ View Task List (PDF)](Homework%20Deliverables/Fall%202025/05%20-%20Tasklist/Tasklist_Adaptabrawl.pdf)** · [Markdown task list](Homework%20Deliverables/Fall%202025/05%20-%20Tasklist/Tasklist.md)  
**[📅 View Timeline & Effort Matrix (PDF)](Homework%20Deliverables/Fall%202025/06%20-%20Milestone%2C%20Timeline%2C%20and%20Effort%20Matrix/Assignment_6_Adaptabrawl_Milestones_Timeline_Effort_Matrix.pdf)** · [Milestones (PDF)](Homework%20Deliverables/Fall%202025/06%20-%20Milestone%2C%20Timeline%2C%20and%20Effort%20Matrix/Assignment_6_Adaptabrawl_Milestones.pdf) · [Timeline (PDF)](Homework%20Deliverables/Fall%202025/06%20-%20Milestone%2C%20Timeline%2C%20and%20Effort%20Matrix/Assignment_6_Adaptabrawl_Timeline.pdf) · [Effort matrix (PDF)](Homework%20Deliverables/Fall%202025/06%20-%20Milestone%2C%20Timeline%2C%20and%20Effort%20Matrix/Assignment_6_Adaptabrawl_Effort_Matrix.pdf)

---

### 6. ABET Concerns Essay (Assignment #7)

Analysis of professional constraints and considerations:

- **Economic:** Budget constraints, monetization strategy
- **Legal:** Licensing, intellectual property, EULA compliance
- **Ethical:** Fair play, accessibility, inclusive design
- **Security:** Data protection, cheat prevention
- **Social:** Community building, toxicity mitigation
- **Environmental:** Energy efficiency, sustainability

**[📄 View ABET Concerns Essay (PDF)](Homework%20Deliverables/Fall%202025/07%20-%20Project%20Constraint%20Essay/Assignment_7_Adaptabrawl_Constraints_Essay.pdf)**

---

### 7. Fall Design Presentation (Assignment #8)

Comprehensive PowerPoint presentation covering:

- Project overview and motivation
- Design diagrams and architecture
- Implementation progress
- ABET considerations
- Future roadmap

**[🎤 View Fall Design Presentation](Homework%20Deliverables/Fall%202025/08%20-%20Fall%20Design%20Presentation/Assignment_8_Adaptabrawl_Fall_Design_Presentation.pptx)**

---

### 8. Video Presentation (Assignment #9)

Recorded demonstration and walkthrough of the project, including:

- Live gameplay demonstration
- Code architecture overview
- Feature highlights
- Team member contributions

**[📹 Watch Video Presentation](Homework%20Deliverables/Fall%202025/09%20-%20Video%20Presentation%20and%20Peer-Reviews/Adaptabrawl_Video_Presentation.mp4)**

---

## Spring 2026 final deliverables

| Artifact                | Link                                                                                                                                                                                                                                                                                                                                                             |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Final Design Report** | [Adaptabrawl_Final_Design_Report.md](Homework%20Deliverables/Spring%202026/07%20-%20Final%20Design%20Report/Adaptabrawl_Final_Design_Report.md)                                                                                                                                                                                                                  |
| **Test plan**           | [TestPlan.md](Homework%20Deliverables/Spring%202026/01%20-%20Test%20Plan/TestPlan.md)                                                                                                                                                                                                                                                                            |
| **User documentation**  | [User_Docs.md](Homework%20Deliverables/Spring%202026/02%20-%20User%20Docs/User_Docs.md) · [User manual](Homework%20Deliverables/Spring%202026/02%20-%20User%20Docs/User_Manual.md) · [Getting started](Homework%20Deliverables/Spring%202026/02%20-%20User%20Docs/Getting_Started.md) · [FAQ](Homework%20Deliverables/Spring%202026/02%20-%20User%20Docs/FAQ.md) |
| **Expo presentation**   | [AdaptaBrawl_Expo_Presentation.pptx](Homework%20Deliverables/Spring%202026/03%20-%20Expo%20Presentation/AdaptaBrawl_Expo_Presentation.pptx)                                                                                                                                                                                                                      |
| **Expo demo (video)**   | [Adaptabrawl_Expo_Demo.mp4](Miscellaneous/Media/Video/Adaptabrawl_Expo_Demo.mp4) — screen capture from the Spring 2026 Senior Design Expo / showcase                                                                                                                                                                                                             |
| **Expo poster**         | [AdaptaBrawl_Expo_Poster.pdf](Homework%20Deliverables/Spring%202026/05%20-%20Expo%20Poster/AdaptaBrawl_Expo_Poster.pdf)                                                                                                                                                                                                                                          |
| **Self assessments**    | [Spring 2026 folder](Homework%20Deliverables/Spring%202026/06%20-%20Self%20Assessment/)                                                                                                                                                                                                                                                                          |

---

## 📹 Demo Video

### Fall 2025 — course video assignment

<div align="center">

**[▶️ Watch Fall video presentation (MP4)](Homework%20Deliverables/Fall%202025/09%20-%20Video%20Presentation%20and%20Peer-Reviews/Adaptabrawl_Video_Presentation.mp4)**

_Gameplay, architecture overview, and team walkthrough (senior design fall deliverable)._

</div>

### Spring 2026 expo demo

**[▶️ Adaptabrawl_Expo_Demo.mp4](Miscellaneous/Media/Video/Adaptabrawl_Expo_Demo.mp4)** — recording from the **Spring 2026 Senior Design Expo** (live booth / showcase demo). Use this clip when you need a short, event-focused demonstration of the final build.

> On GitHub, open the file page and use the built-in player, or **Download** the raw file. Large videos may use [Git LFS](https://git-lfs.com/) if you push them to the remote.

---

## 💰 Budget

### Financial Summary

**Recorded project spend (Spring 2026 final report):** **$34** (e.g. asset purchase, poster printing). **Software stack:** $0 for Unity Personal, Mirror, GitHub free tier, and standard dev tools.

See line-item detail in the [Final Design Report — Summary of Expenses](Homework%20Deliverables/Spring%202026/07%20-%20Final%20Design%20Report/Adaptabrawl_Final_Design_Report.md#9-summary-of-expenses).

### Donated / $0 resources

| Resource                         | Monetary Value (Est.) | Source                |
| -------------------------------- | --------------------- | --------------------- |
| Unity Personal License           | $0 (Free Tier)        | Unity Technologies    |
| Mirror Networking Framework      | $0 (Open Source)      | Mirror Community      |
| Development Tools (VS Code, Git) | $0 (Free)             | Open Source Community |
| GitHub Repository Hosting        | $0 (Free Tier)        | GitHub                |
| **Total donated (software)**     | **$0**                | —                     |

---

## 📚 Appendix

### 📌 Code Repository

**Primary Repository:** [https://github.com/Kartavya904/Adaptabrawl-Senior-Design](https://github.com/Kartavya904/Adaptabrawl-Senior-Design)

### 📖 References & Citations

1. **Unity Documentation** — Unity Technologies. _Unity User Manual_ (6000.x / project LTS). [https://docs.unity3d.com/](https://docs.unity3d.com/)
2. **Mirror Networking** — Mirror Community. _Mirror Networking Documentation_. [https://mirror-networking.com/](https://mirror-networking.com/)
3. **C# Programming Guide** — Microsoft. _C# Documentation_. [https://docs.microsoft.com/en-us/dotnet/csharp/](https://docs.microsoft.com/en-us/dotnet/csharp/)
4. **Game Design Patterns** — Nystrom, R. (2014). _Game Programming Patterns_. Genever Benning.
5. **Fighting Game Design** — Sirlin, D. _Playing to Win: Becoming the Champion_. [http://www.sirlin.net/ptw](http://www.sirlin.net/ptw)

### 🤝 Meeting Notes

<details>
<summary><b>Team Meeting Schedule & Notes</b></summary>

**Regular Meetings:** Almost Every Friday & Sunday, 12:00 PM - 8:00 PM

#### Meeting Log

- **Week 1-2:** Project ideation and scope definition
- **Week 3-4:** Requirements gathering and design diagrams
- **Week 5-6:** Architecture planning and task distribution
- **Week 7-10:** Core system implementation
- **Week 11-12:** Integration and testing
- **Week 13-14:** Documentation and presentation preparation

</details>

### ⏱️ Effort Justification

Each team member has contributed substantial time across **Fall 2025 and Spring 2026** (see the Final Design Report for per-semester hour summaries and evidence links).

**Types of Contributions:**

- Requirements analysis and design documentation
- Software implementation and coding
- Testing, debugging, and quality assurance
- Documentation preparation (essays, diagrams, presentations)
- Team meetings and coordination
- Research and learning new technologies
- Code reviews and collaboration

**Evidence Sources:**

- **Git Repository:** Commit history showing individual contributions ([GitHub Insights](https://github.com/Kartavya904/Adaptabrawl-Senior-Design/graphs/contributors))
- **Homework Deliverables:** Individual assignments and essays documented in this repository
- **Meeting Attendance:** Regular Friday/Sunday team meetings throughout the semester
- **Assignment Submissions:** Timestamps and contribution records for fall and spring deliverables
- **Self-Assessment Essays:** Individual reflections detailing personal contributions and time investment

**Team Contribution Areas:**

- **Kartavya Singh:** Netcode, infrastructure, repository management
- **Saarthak Sinha:** Combat systems, core architecture, tooling
- **Kanav Shetty:** UI/UX design, visual effects, stage design
- **Yash Ballabh:** Testing frameworks, CI/CD, quality assurance

---

## 🔗 Quick Links

| Resource                   | Link                                                                                                                                                   |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 🌐 **GitHub Repository**   | [Adaptabrawl-Senior-Design](https://github.com/Kartavya904/Adaptabrawl-Senior-Design)                                                                  |
| ⬇️ **Windows build (zip)** | [Direct download — Adaptabrawl-Windows.zip](https://github.com/Kartavya904/Adaptabrawl-Senior-Design/releases/latest/download/Adaptabrawl-Windows.zip) |
| 📄 **Final Design Report** | [Adaptabrawl_Final_Design_Report.md](Homework%20Deliverables/Spring%202026/07%20-%20Final%20Design%20Report/Adaptabrawl_Final_Design_Report.md)        |
| 🎪 **Expo demo (MP4)**     | [Adaptabrawl_Expo_Demo.mp4](Miscellaneous/Media/Video/Adaptabrawl_Expo_Demo.mp4)                                                                       |
| 📧 **Contact**             | singhk6@mail.uc.edu                                                                                                                                    |
| 🎓 **University**          | [University of Cincinnati](https://www.uc.edu/)                                                                                                        |
| 🎮 **Unity**               | [Unity Official Site](https://unity.com/)                                                                                                              |
| 🔧 **Mirror Networking**   | [Mirror Documentation](https://mirror-networking.com/)                                                                                                 |

---

## 📝 License

This project is developed as part of the University of Cincinnati Senior Design course. All rights reserved by the team members.

---

<div align="center">

### 🌟 Adaptabrawl — Where Strategy Meets Combat 🌟

**Made with ❤️ by Team Adaptabrawl**

_Fall 2025 – Spring 2026 Senior Design Project_

[⬆️ Back to Top](#-adaptabrawl)

</div>
