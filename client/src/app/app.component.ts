import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './core/layout/header/header.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent],
  template: `
    <app-header></app-header>
    
    <div class="main-content">
      <router-outlet></router-outlet>
    </div>
  `,
  styles: `
    .main-content {
      /* Pushes footer down if we add one later, accounting for header height */
      min-height: calc(100vh - 75px); 
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {}