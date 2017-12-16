import { BrowserModule } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { HttpModule }    from '@angular/http';
import { FormsModule }   from '@angular/forms';
import { RouterModule }   from '@angular/router';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatListModule, MatIconModule, MatButtonModule, MatToolbarModule, MatCheckboxModule, MatTableModule, MatProgressBarModule, MatProgressSpinnerModule, MatPaginatorModule, MatInputModule, MatTabsModule, MatCardModule, MatGridListModule, MatSelectModule, MatSnackBarModule } from '@angular/material';


import { AppComponent } from './app.component';
import { WorkflowSearchComponent } from './workflow-search/workflow-search.component';


@NgModule({
  declarations: [
    AppComponent,
    WorkflowSearchComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    HttpModule,
    CommonModule,
    BrowserAnimationsModule,
    MatInputModule,
    MatListModule,
    MatIconModule,
    MatButtonModule, 
    MatTabsModule,
    MatToolbarModule,
    MatCheckboxModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatCardModule,
    MatSelectModule,
    MatGridListModule,
    MatTableModule,
    MatSnackBarModule,
    MatPaginatorModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
