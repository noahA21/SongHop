import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive],
  template: `
    <header class="main-header">
      <div class="nav-container">
        <a routerLink="/" class="brand-logo" aria-label="SongHop Home">
          <span aria-hidden="true">🎵</span> SongHop
        </a>
        
        <nav aria-label="Global">
          <ul class="nav-links">
            <li>
              <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}">
                Play
              </a>
            </li>
            <li>
              <a routerLink="/database" routerLinkActive="active">
                Database
              </a>
            </li>
          </ul>
        </nav>
      </div>
    </header>
  `,
  styles: `
    .main-header {
      background-color: #1f2937;
      border-bottom: 1px solid #374151;
      padding: 1rem 0;
    }
    .nav-container {
      max-width: 900px;
      margin: 0 auto;
      padding: 0 1.5rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .brand-logo {
      font-size: 1.5rem;
      font-weight: 800;
      color: #f3f4f6;
      text-decoration: none;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
    /* Strict accessibility focus management */
    .brand-logo:focus-visible {
      outline: 3px solid #60a5fa;
      outline-offset: 4px;
      border-radius: 4px;
    }
    .nav-links {
      list-style: none;
      display: flex;
      gap: 1.5rem;
      margin: 0;
      padding: 0;
    }
    .nav-links a {
      color: #9ca3af;
      text-decoration: none;
      font-weight: 500;
      padding: 0.5rem 0.75rem;
      border-radius: 6px;
      transition: color 0.2s, background-color 0.2s;
    }
    .nav-links a:hover {
      color: #f3f4f6;
      background-color: #374151;
    }
    .nav-links a.active {
      color: #60a5fa;
      background-color: rgba(96, 165, 250, 0.1);
    }
    .nav-links a:focus-visible {
      outline: 3px solid #60a5fa;
      outline-offset: 2px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderComponent {}