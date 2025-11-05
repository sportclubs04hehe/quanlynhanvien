import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DonMyListComponent } from './don-my-list/don-my-list.component';

@Component({
  selector: 'app-donyeucau',
  standalone: true,
  imports: [CommonModule, DonMyListComponent],
  templateUrl: './donyeucau.component.html',
  styleUrl: './donyeucau.component.css'
})
export class DonyeucauComponent {
  // TODO: Add tabs navigation later
  // For now, just show don-my-list
}
