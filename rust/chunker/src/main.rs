use std::env;
use std::fs;
use fastcdc::*;

fn main() {
    let input_file_name = env::args().nth(1).expect("Please provide a file name");
    let content = fs::read(input_file_name)
        .expect("Could not read file");

    let min_size = env::args().nth(2).expect("Please provide a min chunk size")
        .parse::<usize>().expect("Not a number");
    let avg_size = env::args().nth(3).expect("Please provide an average chunk size")
        .parse::<usize>().expect("Not a number");
    let max_size = env::args().nth(4).expect("Please provide a max chunk size")
        .parse::<usize>().expect("Not a number");

    let chunker = FastCDC::new(&content, min_size, avg_size, max_size);

    for chunk in chunker {
        println!("{}", chunk.length);
    }
}
