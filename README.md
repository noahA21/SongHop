# 🎵 SongHop

SongHop is a full-stack, data driven graph traversal game where players navigate real world musical collaboration networks. Starting from a randomized artist, players must  discover which artists are collaborative neighbors to discover a path to a target destination artist,ideally, in the fewest moves possible. 

The project is actively deployed and can be played live here:
👉 **[Live Website](https://delightful-dune-0bdd6f010.7.azurestaticapps.net)**

---

## Architecture & Engineering

This project demonstrates decoupled full stack architecture, clean code principles, and efficient relational graph scaling.

* **Decoupled Modern Stack:** Built using an **Angular 17+** single page application frontend and a **C# 10 ASP.NET** backend.
* **Lightweight API Design:** Utilizes a decentralized **"Scorekeeper" model** keeping the backend stateless, scalable, and highly performant.
* **Deterministic State Integrity:** Implemented custom state preservation mechanics ensuring that the 5 expanded neighbor nodes at each step remain completely consistent and persistent. This prevents data rerolling or structural exploitation if a player utilizes the backtrack feature.
* **Hint Engine:** AN explicit connection hints system designed to keep the game accessible for players who might not have deep knowledge of music trivia.
* **Split-Screen Layout:** Features a professional grade interface that cleanly isolates the various sections of information.
* **Multi Cloud Automated CI/CD:** Leverages a modern multi track deployment pipeline. Frontend builds automatically deploy to **Azure Static Web Apps** via GitHub actions, while the backend API is automatically packaged into a Linux virtual environment via **Docker** and deployed to **Render**.

---

## Tech Stack

* **Frontend:** Angular 17+, TypeScript, SCSS, Angular Signals (Reactive State)
* **Backend:** C# 10, ASP.NET Core Minimal APIs, Entity Framework Core
* **Database:** PostgreSQL (Hosted via serverless **Neon DB**)
* **Containerization:** Docker
* **Hosting:** Azure Static Web Apps (Frontend) & Render (Backend Container)

---

## ⚙️ Local Development Setup

Follow these instructions to clone, configure, and execute the entire platform locally.

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/)
* [Node.js (v18+) & npm](https://nodejs.org/)
* Angular CLI (`npm install -g @angular/cli`)
* A local postgreSQL server instance running(or run the docker-compose.yml in the infra directory)

### 1. Backend API Pipeline
1. Navigate to the server directory:
   ```bash
   cd server/SongHop.Api

Open `appsettings.json` and configure your local PostgreSQL connection string under `ConnectionStrings:DefaultConnection`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=songhop;Username=postgres;Password=your_local_password;Port=5432;"
     }
   }
```

Execute the EF Core migrations to automatically build the empty graph tables:

dotnet ef database update
---

### Step 2: Populate the Graph via the ETL Pipeline
To ingest real world artist relationships, you must run the Last.fm / MusicBrainz ETL tool on your newly built schema(step 1).

1. Navigate to your ETL project workspace directory within the codebase.
2. Open its configuration file or environment variables and ensure its destination target connection string matches your local `songhop` PostgreSQL database configured in Step 1.
3. Execute the ETL application to fetch data from the MusicBrainz and Last.fm datasets and map them directly into your database relational tables:
   ```dotnet run``` in ```etl/SongHopCrawler```
this will take a few minutes.

### Step 3: Launch the Backend REST API
Change back to the API directory:

   ```bash
   cd ../../SongHop.Api
```

2. Start the local .Net backend server:
   ``` dotnet run```
Default is http://localhost:5017.

### Step 4: Launch the Frontend
Open a new terminal and navigate to the client application root folder:

``` cd client ```
2. Install the required Node.js workspace dependencies:
   ```npm install```
Open src/environments/environment.ts and verify that the apiUrl points directly to your running local backend port:

```TypeScript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5017/v1'
};
```
4. Boot up the local Angular server:
   ```ng serve```
Open a web browser and navigate to http://localhost:4200 and have fun playing the game!




