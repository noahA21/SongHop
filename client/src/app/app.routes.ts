import { Routes } from '@angular/router';
import { GameBoardComponent } from './features/game-board/game-board.component';

export const routes: Routes = [
  // Direct route to your game board feature
  { path: '', component: GameBoardComponent },
  // Fallback redirect for broken links
  { path: '**', redirectTo: '' }
];