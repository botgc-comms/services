const gulp = require('gulp');
const concat = require('gulp-concat');
const uglify = require('gulp-uglify');
const rename = require('gulp-rename');

const paths = {
    vendor: [
        './wwwroot/lib/jquery/dist/jquery.min.js',
        './wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
        './wwwroot/lib/jquery-validation/dist/jquery.validate.min.js',
        './wwwroot/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js'
    ],
    app: [
        './wwwroot/js/getaddress-autocomplete.js',
        './wwwroot/js/fingerprintjs.js',
        './wwwroot/js/membership-form.js'
    ]
};

const outputPath = './wwwroot/js/dist/';

async function clean() {
    const { deleteAsync } = await import('del');
    return deleteAsync([`${outputPath}*.js`]);
}

function vendorScripts() {
    return gulp.src(paths.vendor)
        .pipe(concat('vendor.bundle.js'))
        .pipe(gulp.dest(outputPath))
        .pipe(uglify())
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest(outputPath));
}

function appScripts() {
    return gulp.src(paths.app)
        .pipe(concat('membership.bundle.js'))
        .pipe(gulp.dest(outputPath))
        .pipe(uglify({ mangle: { toplevel: true } }))
        .pipe(rename({ suffix: '.min' }))
        .pipe(gulp.dest(outputPath));
}

const build = gulp.series(clean, gulp.parallel(vendorScripts, appScripts));

exports.default = build;
