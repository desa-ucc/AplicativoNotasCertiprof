import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CertiprofComponent } from './certiprof.component';

describe('CertiprofComponent', () => {
  let component: CertiprofComponent;
  let fixture: ComponentFixture<CertiprofComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CertiprofComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CertiprofComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
