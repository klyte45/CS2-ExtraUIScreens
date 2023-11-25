
import 'coherent-gameface-scrollable-container';
import './GameScrollComponent.scss'
import { Component } from 'react';

declare global {
    namespace JSX {
        interface IntrinsicElements {
            'gameface-scrollable-container': GamefaceScrollableContainer
            'component-slot': GamefaceComponentSlotProps
        }
    }
}

interface GamefaceScrollableContainer extends React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> {
    automatic?: boolean,
    class?: string
}
interface GamefaceComponentSlotProps extends React.DetailedHTMLProps<React.HTMLAttributes<HTMLElement>, HTMLElement> {
    'data-name': string
}

export class GameScrollComponent extends Component<{}>{
    render() {
        return <gameface-scrollable-container class="no-arrows" automatic>
            <component-slot data-name="scrollable-content">
                {this.props.children}
            </component-slot>
        </gameface-scrollable-container>
    }
}