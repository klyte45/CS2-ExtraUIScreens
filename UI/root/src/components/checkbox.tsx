import { Component } from "react";

interface CheckboxProps {
    title: string;
    isChecked: () => boolean;
    onClick: () => void;
}

export class Checkbox extends Component<CheckboxProps, { checked: boolean }> {
    constructor(props: CheckboxProps) {
        super(props);
        this.state = {
            checked: this.props.isChecked()
        }
    }
    render() {
        const { title, onClick } = this.props;
        return (
            <>
                <div className="field__MBOM9 field__UuCZq" onClick={() => {
                    onClick();
                    return this.setState({ checked: this.props.isChecked() });
                }}>
                    <div className="label__DGc7_ label__ZLbNH">
                        {title}
                    </div>
                    <div className={`toggle__ccalN item-mouse-states__FmiyB toggle__th_34 ${this.state.checked ? "checked" : "unchecked"}`} >
                        <div className={`checkmark__NXVuH ${this.state.checked ? "checked" : ""}`} ></div>
                    </div>
                </div>
            </>
        );
    }
}

interface CheckboxTitlelessProps {
    isChecked: () => boolean;
    onClick: (newVal: boolean) => void;
}

export class CheckboxTitleless extends Component<CheckboxTitlelessProps, { checked: () => boolean }> {
    constructor(props: CheckboxProps) {
        super(props);
        this.state = {
            checked: props.isChecked
        }
    }
    render() {
        const { onClick } = this.props;
        const isChecked = this.state.checked();
        return (<><div className={`toggle__ccalN item-mouse-states__FmiyB toggle__th_34 ${isChecked ? "checked" : "unchecked"}`} onClick={() => onClick(!isChecked)}>
            <div className={`checkmark__NXVuH ${isChecked ? "checked" : ""}`} ></div>
        </div>
        </>);
    }
}