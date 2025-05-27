<template>
  <div/>
</template>

<script>
import $ from 'jquery';
import 'round-slider';

export default {
  name: 'RoundSlider',

  props: {
    // Basic props (frequently used)
    min: {
      type: [String, Number],
      default: 0
    },
    max: {
      type: [String, Number],
      default: 20000
    },
    step: {
      type: [String, Number],
      default: 100
    },
    value: {
      type: [String, Number],
      default: null
    },
    radius: {
      type: [String, Number],
      default: 105
    },
    width: {
      type: [String, Number],
      default: 20
    },
    lineCap: {
      type: String,
      default: "butt",
      validator(cap) {
        return validateProp('lineCap', cap);
      },
    },
    startAngle: {
      type: [String, Number],
      default: 0
    },
    endAngle: {
      type: [String, Number],
      default: "+360"
    },

    // UI appearance related props
    borderWidth: {
      type: [String, Number],
      default: 0
    },
    borderColor: {
      type: String,
      default: "inherit"
    },
    pathColor: {
      type: String,
      default: "#EEE"
    },
    rangeColor: {
      type: String,
      default: "#ff8ba7"
    },
    tooltipColor: {
      type: String,
      default: "inherit"
    },

    // Behaviour related props
    sliderType: {
      type: String,
      default: "min-range",
      validator(type) {
        return validateProp('sliderType', type);
      },
    },
    circleShape: {
      type: String,
      default: "full",
      validator(shape) {
        return validateProp('circleShape', shape);
      },
    },
    animation: {
      type: [String, Boolean],
      default: true
    },
    readOnly: {
      type: [String, Boolean],
      default: false
    },
    disabled: {
      type: [String, Boolean],
      default: false
    },

    // Miscellaneous
    handleSize: {
      type: [String, Number],
      default: "+0"
    },
    handleShape: {
      type: String,
      default: "round",
      validator(shape) {
        return validateProp('handleShape', shape);
      },
    },
    showTooltip: {
      type: [String, Boolean],
      default: true
    },
    editableTooltip: {
      type: [String, Boolean],
      default: true
    },
    keyboardAction: {
      type: [String, Boolean],
      default: true
    },
    mouseScrollAction: {
      type: [String, Boolean],
      default: false
    },

    // Usecase related props
    startValue: {
      type: [String, Number],
      default: null
    },

    // Events
    create: {
      type: Function,
      default: null,
    },
    beforeValueChange: {
      type: Function,
      default: null,
    },
    change: {
      type: Function,
      default: null,
    },
    update: {
      type: Function,
      default: null,
    },
    valueChange: {
      type: Function,
      default: null,
    },
    tooltipFormat: {
      type: Function,
      default: null,
    }
  },

  computed: {
    control() {
      return $(this.$el);
    },

    instance() {
      return this.control.data('roundSlider');
    },

    allProps() {
      if (this.$props) {
        return this.$props;
      }
      // for the vue lower versions
      const keys = Object.keys(this.$options.props);
      const props = keys.reduce((propsObj, key) => {
        const obj = {};
        obj[key] = this[key];
        return Object.assign(propsObj, obj);
      }, {});
      return props;
    }
  },

  mounted() {
    // below are the default props to overwrite from base roundSlider
    const defaultProps = {
      svgMode: true
    };
    // merge the actual props witht the default props then initialize the component
    const options = Object.assign(defaultProps, this.allProps);

    this.control
        .roundSlider(options)
        .on("update", ({value}) => {
          this.$emit('input', value);
        });

    // all the props from round-slider will support the one-way data binding
    // so, watch all the props for the changes to reflect in the component
    this.watchProps();
  },

  destroyed() {
    this.control.roundSlider("destroy");
  },

  methods: {
    watchProps() {
      // whenever the prop changed, update the prop with the base 'roundSlider' component
      const props = Object.keys(this.allProps);
      props.forEach((prop) => {
        this.$watch(prop, value => {
          this.updateProp(prop, value);
        });
      }, this);
    },

    updateProp(prop, value) {
      this.instance.option(prop, value);
    },
  }

}

// the possible values for the string type props
// #: later this can be imported from the base roundSlider
const possibleValues = {
  lineCap: ['butt', 'round', 'square', 'none'],
  sliderType: ['min-range', 'range', 'default'],
  circleShape: [
    "full", "pie", "half-top", "half-bottom", "half-left", "half-right",
    "quarter-top-left", "quarter-top-right", "quarter-bottom-right", "quarter-bottom-left"
  ],
  handleShape: ["round", "square", "dot"]
}

const validateProp = (prop, value) => {
  const allValues = possibleValues[prop];
  if (allValues.indexOf(value) === -1) {
    const msg = `custom validator check failed for prop "${prop}" with value "${value}"`;
    const info = `\n\n---> The possible values are \n\t\t* ${allValues.join('\n\t\t* ')}\n\n`;
    console.error(("[Vue warn]: Invalid prop: " + msg + info));
  }
  return true;
}
</script>

<!--<style src='../node_modules/round-slider/dist/roundslider.min.css'></style>-->

<style>
/* some UI customization of roundSlider for Vue */
/* #: later this can be applied for base component also */

.rs-handle {
  background-color: #f3f3f3;
  box-shadow: 0px 0px 4px 0px #000;
}

.rs-tooltip-text {
  font-size: 26px;
  font-weight: 500;
  font-family: Avenir, Tahoma, Verdana, sans-serif;
}

.rs-animation .rs-transition {
  transition: all 0.5s ease-in-out 0s;
}

.rs-tooltip.rs-hover, .rs-tooltip.rs-edit:hover {
  border: 1px solid #cacaca;
}

.rs-edge, .rs-handle, .rs-ie {
  -ms-touch-action: none;
  touch-action: none
}

.rs-control {
  position: relative;
  outline: 0 none
}

.rs-container {
  position: relative
}

.rs-control *, .rs-control :after, .rs-control :before {
  box-sizing: border-box
}

.rs-animation .rs-transition {
  transition: all .5s linear 0s
}

.rs-bar {
  -webkit-transform-origin: 100% 50%;
  -ms-transform-origin: 100% 50%;
  transform-origin: 100% 50%
}

.rs-control .rs-overlay1, .rs-control .rs-overlay2, .rs-control .rs-split .rs-path {
  -webkit-transform-origin: 50% 100%;
  -ms-transform-origin: 50% 100%;
  transform-origin: 50% 100%
}

.rs-control .rs-overlay {
  -webkit-transform-origin: 100% 100%;
  -ms-transform-origin: 100% 100%;
  transform-origin: 100% 100%
}

.rs-rounded .rs-seperator, .rs-split .rs-path {
  background-clip: padding-box
}

.rs-disabled {
  opacity: .35
}

.rs-inner-container {
  height: 100%;
  width: 100%;
  position: absolute;
  top: 0;
  overflow: hidden
}

.rs-control .rs-quarter div.rs-block {
  height: 200%;
  width: 200%
}

.rs-control .rs-half.rs-bottom div.rs-block, .rs-control .rs-half.rs-top div.rs-block {
  height: 200%;
  width: 100%
}

.rs-control .rs-half.rs-left div.rs-block, .rs-control .rs-half.rs-right div.rs-block {
  height: 100%;
  width: 200%
}

.rs-control .rs-bottom .rs-block {
  top: auto;
  bottom: 0
}

.rs-control .rs-right .rs-block {
  right: 0
}

.rs-block.rs-outer {
  border-radius: 1000px
}

.rs-block {
  height: 100%;
  width: 100%;
  display: block;
  position: absolute;
  top: 0;
  overflow: hidden;
  z-index: 3
}

.rs-block .rs-inner {
  border-radius: 1000px;
  display: block;
  height: 100%;
  width: 100%;
  position: relative
}

.rs-overlay {
  width: 50%
}

.rs-overlay1, .rs-overlay2 {
  width: 100%
}

.rs-overlay, .rs-overlay1, .rs-overlay2 {
  position: absolute;
  background-color: #fff;
  z-index: 3;
  top: 0;
  height: 50%
}

.rs-bar {
  display: block;
  position: absolute;
  bottom: 0;
  height: 0;
  z-index: 10
}

.rs-bar.rs-rounded {
  z-index: 5
}

.rs-bar .rs-seperator {
  height: 0;
  display: block;
  float: left
}

.rs-bar:not(.rs-rounded) .rs-seperator {
  border-left: none;
  border-right: none
}

.rs-bar.rs-start .rs-seperator {
  border-top: none
}

.rs-bar.rs-end .rs-seperator {
  border-bottom: none
}

.rs-bar.rs-start.rs-rounded .rs-seperator {
  border-radius: 0 0 1000px 1000px
}

.rs-bar.rs-end.rs-rounded .rs-seperator {
  border-radius: 1000px 1000px 0 0
}

.rs-full .rs-bar, .rs-half .rs-bar {
  width: 50%
}

.rs-half.rs-left .rs-bar, .rs-half.rs-right .rs-bar, .rs-quarter .rs-bar {
  width: 100%
}

.rs-full .rs-bar, .rs-half.rs-left .rs-bar, .rs-half.rs-right .rs-bar {
  top: 50%
}

.rs-bottom .rs-bar {
  top: 0
}

.rs-half.rs-right .rs-bar, .rs-quarter.rs-right .rs-bar {
  right: 100%
}

.rs-classic-mode .rs-path {
  display: block;
  height: 100%;
  width: 100%
}

.rs-split .rs-path {
  border-radius: 1000px 1000px 0 0;
  overflow: hidden;
  height: 50%;
  position: absolute;
  top: 0;
  z-index: 2
}

.rs-control .rs-svg-container {
  display: block;
  position: absolute;
  top: 0
}

.rs-control .rs-bottom .rs-svg-container {
  top: auto;
  bottom: 0
}

.rs-control .rs-right .rs-svg-container {
  right: 0
}

.rs-tooltip {
  position: absolute;
  cursor: default;
  border: 1px solid transparent;
  z-index: 10
}

.rs-full .rs-tooltip {
  top: 50%;
  left: 50%
}

.rs-bottom .rs-tooltip {
  top: 0
}

.rs-top .rs-tooltip {
  bottom: 0
}

.rs-right .rs-tooltip {
  left: 0
}

.rs-left .rs-tooltip {
  right: 0
}

.rs-half.rs-bottom .rs-tooltip, .rs-half.rs-top .rs-tooltip {
  left: 50%
}

.rs-half.rs-left .rs-tooltip, .rs-half.rs-right .rs-tooltip {
  top: 50%
}

.rs-tooltip .rs-input {
  outline: 0 none;
  border: none;
  background: 0 0
}


.rs-tooltip.rs-editable {
  padding: 5px 8px
}

.rs-tooltip.rs-editable:hover, .rs-tooltip.rs-hover {
  border-color: #aaa;
  cursor: pointer
}

.rs-container.rs-readonly .rs-tooltip.rs-editable:hover, .rs-control.rs-dragging .rs-tooltip.rs-editable:hover {
  border-color: transparent;
  cursor: default
}

.rs-tooltip.rs-center {
  margin: 0 !important
}

.rs-half.rs-bottom .rs-tooltip.rs-center, .rs-half.rs-top .rs-tooltip.rs-center {
  -webkit-transform: translate(-50%, 0);
  -ms-transform: translate(-50%, 0);
  transform: translate(-50%, 0)
}

.rs-half.rs-left .rs-tooltip.rs-center, .rs-half.rs-right .rs-tooltip.rs-center {
  -webkit-transform: translate(0, -50%);
  -ms-transform: translate(0, -50%);
  transform: translate(0, -50%)
}

.rs-full .rs-tooltip.rs-center {
  -webkit-transform: translate(-50%, -50%);
  -ms-transform: translate(-50%, -50%);
  transform: translate(-50%, -50%)
}

.rs-tooltip.rs-reset {
  margin: 0 !important;
  top: 0 !important;
  left: 0 !important
}

.rs-handle {
  cursor: move;
  border-radius: 1000px;
  outline: 0 none;
  float: left;
  display: -ms-flexbox;
  display: flex;
  -ms-flex-align: center;
  align-items: center;
  -ms-flex-pack: center;
  justify-content: center
}

.rs-container.rs-readonly .rs-handle, .rs-control.rs-dragging .rs-handle {
  cursor: default
}

.rs-handle.rs-handle-square {
  border-radius: 0
}

.rs-handle-dot-inner {
  border-radius: 1000px;
  padding: 20%
}

.rs-seperator {
  border: 1px solid #aaa
}

.rs-border {
  border: 1px solid #aaa
}

.rs-path-color {
  background-color: #fff
}

.rs-range-color {
  background-color: #54bbe0
}

.rs-bg-color {
  background-color: #fff
}


.rs-handle-dot {
  background-color: #fff;
  border: 1px solid #838383
}

.rs-handle-dot-inner {
  background-color: #838383
}

.rs-path-inherited .rs-path {
  opacity: .25
}
</style>