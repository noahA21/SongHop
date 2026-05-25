import { Routes } from '@angular/router';
import { GameBoardComponent } from './features/game-board/game-board.component';
import { ArtistSearchComponent } from './features/artist-search/artist-search.component';

export const routes: Routes = [
  { path: '', component: GameBoardComponent },
  { path: 'database', component: ArtistSearchComponent }, // <-- Map our new page layout here
  { path: '**', redirectTo: '' }
];