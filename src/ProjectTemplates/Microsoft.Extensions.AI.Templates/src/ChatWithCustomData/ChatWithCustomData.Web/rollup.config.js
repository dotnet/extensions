import resolve from 'rollup-plugin-node-resolve';

export default {
	input: 'wwwroot/lib.js',
	output: {
		file: 'wwwroot/lib.out.js',
		format: 'iife',
		name: 'lib'
	},
	plugins: [resolve()]
};
