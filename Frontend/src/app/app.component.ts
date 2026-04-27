import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CertiprofComponent } from './certiprof/certiprof.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CertiprofComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'Frontend';
}
