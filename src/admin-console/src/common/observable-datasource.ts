import { DataSource } from '@angular/cdk/collections';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { Observable, SubscribableOrPromise,  } from 'rxjs/Observable';
import { ISubscription } from 'rxjs/Subscription';

export class ObservableDataSource<T> extends DataSource<T> {
  
  get data(): T[] { return this.dataChange.value; }

  constructor(
    private dataChange: BehaviorSubject<T[]>,
    private triggers: any[]
    ) {
    super();
  }
  
  connect(): Observable<T[]> {
    const displayDataChanges : SubscribableOrPromise<T>[] = this.triggers.concat([this.dataChange]);

    return Observable.merge(...displayDataChanges).map(() => {
      return this.data.slice();
    });
  }

  disconnect() {}
}